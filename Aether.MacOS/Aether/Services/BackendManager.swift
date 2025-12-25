import Combine
import Foundation

/// Manages the lifecycle of the .NET backend process
class BackendManager: ObservableObject {
    static let shared = BackendManager()

    // ═══════════════════════════════════════════════════════════════════════════
    // DEV TOGGLE: Set to false for production, true during development
    // When true, assumes backend is running externally (e.g., via `dotnet run`)
    // ═══════════════════════════════════════════════════════════════════════════
    private let useExternalBackend = false  // <-- TOGGLE THIS FOR DEV/PROD

    // ═══════════════════════════════════════════════════════════════════════════
    // ADMIN TOGGLE: Set to true for Debug builds (fixes TCC), false for Release (if signed)
    // ═══════════════════════════════════════════════════════════════════════════
    private let launchAsAdmin = false  // <-- TOGGLE THIS FOR ADMIN LAUNCH
    // ═══════════════════════════════════════════════════════════════════════════

    private var process: Process?
    @Published var isRunning = false
    @Published var statusMessage = ""

    private init() {}

    /// Start the backend process
    func start() {
        if useExternalBackend {
            print("⚙️ Dev mode: Using external backend")
            statusMessage = "Dev mode: External backend"
            isRunning = true
            return
        }

        guard process == nil else { return }

        if launchAsAdmin {
            startAsAdmin()
        } else {
            startAsUser()
        }
    }

    // MARK: - Admin Launch (Root)
    private func startAsAdmin() {
        guard let bundleExecutableURL = Bundle.main.executableURL else { return }
        let macOSDir = bundleExecutableURL.deletingLastPathComponent()
        let backendURL = macOSDir.appendingPathComponent("AetherBackend")

        let appSupport = FileManager.default.urls(
            for: .applicationSupportDirectory, in: .userDomainMask
        ).first!
        let dataDir = appSupport.appendingPathComponent("Aether")
        try? FileManager.default.createDirectory(at: dataDir, withIntermediateDirectories: true)

        let resourcePath = Bundle.main.resourcePath ?? ""
        let pluginsPath = resourcePath + "/plugins"

        let realHome: String
        if let pw = getpwuid(getuid()), let homeDir = pw.pointee.pw_dir {
            realHome = String(cString: homeDir)
        } else {
            realHome = "/Users/\(NSUserName())"
        }

        let logFile = dataDir.appendingPathComponent("backend_launch.log").path

        // Construct Command (Single line with semicolons)
        let shellCommand =
            "export HOME=\"\(realHome)\"; export PLUGINS_PATH=\"\(pluginsPath)\"; cd \"\(dataDir.path)\"; \"\(backendURL.path)\" > \"\(logFile)\" 2>&1 & echo $!"

        // Escape for AppleScript string literal
        var escapedCommand = shellCommand.replacingOccurrences(of: "\\", with: "\\\\")
        escapedCommand = escapedCommand.replacingOccurrences(of: "\"", with: "\\\"")

        print("🚀 Launching Backend as Admin...")

        var error: NSDictionary?
        if let scriptObject = NSAppleScript(
            source: "do shell script \"\(escapedCommand)\" with administrator privileges")
        {
            let outputDescriptor = scriptObject.executeAndReturnError(&error)

            if let error = error {
                statusMessage = "Failed to launch (Admin): \(error)"
                print("❌ Admin Launch Error: \(error)")
                return
            }

            if let pidString = outputDescriptor.stringValue, let pid = Int32(pidString) {
                print("✅ Backend started as ROOT (PID: \(pid))")
                statusMessage = "Backend running as Root (PID: \(pid))"
                isRunning = true

                // We fake a Process object to track state simply
                self.process = Process()  // Dummy holder
                self.rootPid = pid
            }
        }
    }

    // MARK: - Standard User Launch (Process)
    private func startAsUser() {
        guard let bundleExecutableURL = Bundle.main.executableURL else {
            statusMessage = "Failed to locate bundle executable"
            return
        }
        let macOSDir = bundleExecutableURL.deletingLastPathComponent()
        let backendURL = macOSDir.appendingPathComponent("AetherBackend")

        guard FileManager.default.fileExists(atPath: backendURL.path) else {
            statusMessage = "Backend not found in bundle"
            return
        }

        let appSupport = FileManager.default.urls(
            for: .applicationSupportDirectory, in: .userDomainMask
        ).first!
        let dataDir = appSupport.appendingPathComponent("Aether")
        try? FileManager.default.createDirectory(at: dataDir, withIntermediateDirectories: true)

        let backendProcess = Process()
        backendProcess.executableURL = backendURL
        backendProcess.currentDirectoryURL = dataDir

        var env = ProcessInfo.processInfo.environment
        if let resourcesPath = Bundle.main.resourcePath {
            env["PLUGINS_PATH"] = resourcesPath + "/plugins"
        }

        let realHome: String
        if let pw = getpwuid(getuid()), let homeDir = pw.pointee.pw_dir {
            realHome = String(cString: homeDir)
        } else {
            realHome = "/Users/\(NSUserName())"
        }
        env["HOME"] = realHome
        backendProcess.environment = env

        let pipe = Pipe()
        backendProcess.standardOutput = pipe
        backendProcess.standardError = pipe

        backendProcess.terminationHandler = { [weak self] process in
            DispatchQueue.main.async {
                self?.isRunning = false
                self?.statusMessage = "Backend stopped (exit: \(process.terminationStatus))"
                print("🔴 Backend terminated with status: \(process.terminationStatus)")
            }
        }

        do {
            try backendProcess.run()
            self.process = backendProcess
            isRunning = true
            statusMessage = "Backend running (PID: \(backendProcess.processIdentifier))"
            print("✅ Backend started (PID: \(backendProcess.processIdentifier))")

            Task.detached {
                for try await line in pipe.fileHandleForReading.bytes.lines {
                    print("[Backend] \(line)")
                }
            }
        } catch {
            statusMessage = "Failed to launch: \(error.localizedDescription)"
            print("❌ \(statusMessage)")
        }
    }

    // Track Root PID
    private var rootPid: Int32?

    /// Helper to read version.json from backend dir
    private func getInstalledVersion(at dir: URL) -> String? {
        let versionFile = dir.appendingPathComponent("version.json")
        guard let data = try? Data(contentsOf: versionFile),
            let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
            let version = json["version"] as? String
        else {
            return nil
        }
        return version
    }

    /// Stop the backend process
    func stop() {
        guard !useExternalBackend else { return }

        // Kill Root Process
        if let pid = rootPid {
            print("🛑 Stopping Root Backend (PID: \(pid))...")
            let killScript = "do shell script \"kill \(pid)\" with administrator privileges"
            NSAppleScript(source: killScript)?.executeAndReturnError(nil)
            rootPid = nil
        }

        // Old Process Cleanup (if any fallback)
        if let process = process, process.isRunning {
            process.terminate()
        }

        self.process = nil
        isRunning = false
        statusMessage = "Backend stopped"
    }

    /// Restart the backend
    func restart() {
        stop()
        // Small delay to ensure process fully terminates
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) { [weak self] in
            self?.start()
        }
    }
}
