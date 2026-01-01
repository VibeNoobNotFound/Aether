import AetherIPC
import Combine
import Foundation

/// Connection state for UI display
enum ConnectionState: Equatable {
    case disconnected
    case connecting
    case connected
    case error(String)

    var isReady: Bool {
        if case .connected = self { return true }
        return false
    }
}

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
    @Published var connectionState: ConnectionState = .disconnected
    @Published var statusMessage = ""

    private var healthCheckTask: Task<Void, Never>?
    private let grpcClient = GrpcClient()

    private init() {}

    /// Start the backend process and begin health probing
    func start() {
        if useExternalBackend {
            print("⚙️ Dev mode: Using external backend")
            statusMessage = "Dev mode: External backend"
            isRunning = true
            startHealthProbing()
            return
        }

        guard process == nil else { return }

        // Kill any stale backend process from previous crash
        killStaleBackend()

        // Update connection state
        connectionState = .connecting

        if launchAsAdmin {
            startAsAdmin()
        } else {
            startAsUser()
        }

        // Start health probing after launching
        startHealthProbing()
    }

    /// Kill any orphaned AetherBackend process that may be holding the port
    private func killStaleBackend() {
        let task = Process()
        task.executableURL = URL(fileURLWithPath: "/usr/bin/pkill")
        task.arguments = ["-f", "AetherBackend"]

        do {
            try task.run()
            task.waitUntilExit()
            if task.terminationStatus == 0 {
                print("🧹 Killed stale AetherBackend process")
                // Brief pause to let port be released
                Thread.sleep(forTimeInterval: 0.5)
            }
        } catch {
            // pkill returns 1 if no process matched, which is fine
            print("ℹ️ No stale backend process found")
        }
    }

    // MARK: - Health Probing with Exponential Backoff

    private func startHealthProbing() {
        healthCheckTask?.cancel()

        healthCheckTask = Task { @MainActor [weak self] in
            guard let self = self else { return }

            var delay: UInt64 = 100_000_000  // Start at 100ms
            let maxDelay: UInt64 = 2_000_000_000  // Cap at 2s
            var attempts = 0
            let maxAttempts = 60  // 30s total timeout approx

            while !Task.isCancelled && attempts < maxAttempts {
                do {
                    // Try to ping backend
                    let response = try await self.grpcClient.client.ping(Aether_Empty())

                    if response.healthy {
                        self.connectionState = .connected
                        print("✅ Backend health check passed")

                        // Continue monitoring in background
                        await self.monitorConnection()
                        return
                    }
                } catch {
                    // Still connecting...
                    attempts += 1
                    print("⏳ Health probe attempt \(attempts): \(error.localizedDescription)")
                }

                // Exponential backoff with jitter
                let jitter = UInt64.random(in: 0..<(delay / 5))
                try? await Task.sleep(nanoseconds: delay + jitter)
                delay = min(delay * 2, maxDelay)
            }

            // Failed to connect after timeout
            if !Task.isCancelled {
                self.connectionState = .error("Failed to connect to backend")
            }
        }
    }

    /// Continuous monitoring after initial connection
    private func monitorConnection() async {
        while !Task.isCancelled && isRunning {
            try? await Task.sleep(nanoseconds: 5_000_000_000)  // 5s interval

            do {
                let response = try await grpcClient.client.ping(Aether_Empty())
                if !response.healthy {
                    await MainActor.run {
                        connectionState = .error("Backend reported unhealthy")
                    }
                }
            } catch {
                await MainActor.run {
                    connectionState = .error("Connection lost")
                }
                break
            }
        }
    }

    /// Retry connection after error
    func retryConnection() {
        connectionState = .connecting
        startHealthProbing()
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

        let logFile = dataDir.appendingPathComponent("server_stdout.log").path

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
                connectionState = .error("Admin launch failed")
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
            connectionState = .error("Bundle not found")
            return
        }
        let macOSDir = bundleExecutableURL.deletingLastPathComponent()
        let backendURL = macOSDir.appendingPathComponent("AetherBackend")

        guard FileManager.default.fileExists(atPath: backendURL.path) else {
            statusMessage = "Backend not found in bundle"
            connectionState = .error("Backend executable missing")
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
                self?.connectionState = .error("Backend stopped unexpectedly")
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
            connectionState = .error("Launch failed: \(error.localizedDescription)")
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
        healthCheckTask?.cancel()

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
        connectionState = .disconnected
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
