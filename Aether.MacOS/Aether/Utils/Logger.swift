import Foundation
import OSLog
import os

final class Logger: @unchecked Sendable {
    static let shared = Logger()
    private let logger = OSLog(subsystem: "com.antigravity.aether", category: "Application")
    private let queue = DispatchQueue(label: "com.antigravity.aether.logger")
    private var fileHandle: FileHandle?

    private init() {
        setupFileLogging()
    }

    private func setupFileLogging() {
        guard
            let appSupport = FileManager.default.urls(
                for: .applicationSupportDirectory, in: .userDomainMask
            ).first
        else { return }
        let logDir = appSupport.appendingPathComponent("Aether/logs")
        let logFile = logDir.appendingPathComponent("frontend.log")

        do {
            try FileManager.default.createDirectory(at: logDir, withIntermediateDirectories: true)

            if !FileManager.default.fileExists(atPath: logFile.path) {
                FileManager.default.createFile(atPath: logFile.path, contents: nil)
            }

            fileHandle = try FileHandle(forWritingTo: logFile)
            fileHandle?.seekToEndOfFile()

            log("Logger initialized. Log file: \(logFile.path)")
        } catch {
            print("Failed to setup file logging: \(error)")
        }
    }

    func log(_ message: String, type: OSLogType = .default) {
        // 1. Console Logging (Unified Logging System) - Thread safe
        os_log("%{public}@", log: logger, type: type, message)

        // 2. File Logging - Serialized via Queue
        let timestamp = ISO8601DateFormatter().string(from: Date())
        let fileMessage = "[\(timestamp)] \(message)\n"

        queue.async {
            if let data = fileMessage.data(using: .utf8) {
                self.fileHandle?.write(data)
            }
        }
    }

    deinit {
        let handle = fileHandle
        queue.sync {
            try? handle?.close()
        }
    }
}
