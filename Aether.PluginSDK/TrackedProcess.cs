namespace Aether.PluginSDK;

/// <summary>
/// Represents a process being tracked for a game session.
/// Used to store process information for later termination if needed.
/// </summary>
public struct TrackedProcess
{
    /// <summary>
    /// The process ID (PID) of the tracked process.
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// The full path to the executable file.
    /// May be null if the path could not be determined (permission issues).
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// The name of the process (without path or extension).
    /// </summary>
    public string? ProcessName { get; set; }

    public override string ToString() =>
        $"[{ProcessId}] {ProcessName ?? "Unknown"} ({ExecutablePath ?? "path unknown"})";
}
