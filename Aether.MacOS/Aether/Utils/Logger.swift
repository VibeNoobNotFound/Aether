import Foundation
import OSLog

/// Simple file logger for Aether frontend
class AetherLogger {
    static let shared = AetherLogger()

    private let fileHandle: FileHandle?
    private let logURL: URL
    private let osLog = OSLog(subsystem: "com.aether.app", category: "general")

    private init() {
        // Get Application Support directory
        let appSupport = FileManager.default.urls(
            for: .applicationSupportDirectory, in: .userDomainMask
        ).first!
        let logsDir = appSupport.appendingPathComponent("Aether/logs/client")

        // Create logs directory if needed
        try? FileManager.default.createDirectory(at: logsDir, withIntermediateDirectories: true)

        // Create timestamped log file
        let timestamp = ISO8601DateFormatter().string(from: Date()).replacingOccurrences(
            of: ":", with: "-")
        logURL = logsDir.appendingPathComponent("client-\(timestamp).log")

        // Create file and get handle
        FileManager.default.createFile(atPath: logURL.path, contents: nil)
        fileHandle = try? FileHandle(forWritingTo: logURL)

        log("🚀 Aether client started")
        log("📁 Log file: \(logURL.path)")
    }

    deinit {
        fileHandle?.closeFile()
    }

    /// Log a message to both console and file
    func log(_ message: String, level: OSLogType = .info) {
        let timestamp = DateFormatter.localizedString(
            from: Date(), dateStyle: .none, timeStyle: .medium)
        let formattedMessage = "[\(timestamp)] \(message)"

        // Log to console/debug
        os_log("%{public}@", log: osLog, type: level, formattedMessage)

        // Log to file
        if let data = (formattedMessage + "\n").data(using: .utf8) {
            fileHandle?.write(data)
        }
    }

    func error(_ message: String) {
        log("[ERR] \(message)", level: .error)
    }

    func warning(_ message: String) {
        log("[WRN] \(message)", level: .default)
    }

    func info(_ message: String) {
        log("[INF] \(message)", level: .info)
    }
}
