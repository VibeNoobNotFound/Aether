using System.Diagnostics;

namespace Aether.PluginSDK.Helpers;

public class ProcessMonitorOptions
{
    /// <summary>
    /// Time to wait before starting to check for the process (ms).
    /// Default: 4000ms.
    /// </summary>
    public int GracePeriodMs { get; set; } = 4000;

    /// <summary>
    /// Interval to poll when searching for the process (ms).
    /// Default: 1000ms.
    /// </summary>
    public int SearchIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Max time to try finding the process before giving up (ms).
    /// Default: 15000ms (15 seconds).
    /// </summary>
    public int MaxSearchTimeMs { get; set; } = 15000;

    /// <summary>
    /// If true, enables heuristic to detect launcher wrappers.
    /// If process exits within LauncherThresholdMs, session remains active (Manual Mode).
    /// Default: false.
    /// </summary>
    public bool EnableLauncherHeuristic { get; set; } = false;

    /// <summary>
    /// Time threshold for launcher heuristic (ms).
    /// Default: 15000ms (15 seconds).
    /// </summary>
    public int LauncherThresholdMs { get; set; } = 15000;
}

public static class ProcessMonitor
{
    private static void Log(Action<string>? logger, string message) => logger?.Invoke(message);

    /// <summary>
    /// Monitors a process by its ID.
    /// </summary>
    public static async Task MonitorByIdAsync(
        string gameId,
        int processId,
        ISessionManager sessionManager,
        Action<string>? logAction = null,
        ProcessMonitorOptions? options = null)
    {
        MonitorInternal(gameId, sessionManager, logAction, options, () =>
        {
            try
            {
                return new[] { Process.GetProcessById(processId) };
            }
            catch
            {
                return Array.Empty<Process>();
            }
        });
    }

    /// <summary>
    /// Monitors a process by its name (exact match).
    /// </summary>
    public static async Task MonitorByNameAsync(
        string gameId,
        string processName,
        ISessionManager sessionManager,
        Action<string>? logAction = null,
        ProcessMonitorOptions? options = null)
    {
        MonitorInternal(gameId, sessionManager, logAction, options, () => 
            Process.GetProcessesByName(processName));
    }

    /// <summary>
    /// Monitors a process by partial name match.
    /// </summary>
    public static async Task MonitorByPartialNameAsync(
        string gameId,
        string processNameFragment,
        ISessionManager sessionManager,
        Action<string>? logAction = null,
        ProcessMonitorOptions? options = null)
    {
        MonitorInternal(gameId, sessionManager, logAction, options, () =>
            Process.GetProcesses()
                .Where(p => p.ProcessName.Contains(processNameFragment, StringComparison.OrdinalIgnoreCase))
                .ToArray());
    }

    private static void MonitorInternal(
        string gameId,
        ISessionManager sessionManager,
        Action<string>? logAction,
        ProcessMonitorOptions? options,
        Func<Process[]> findProcesses)
    {
        options ??= new ProcessMonitorOptions();
        
        // Run on a separate thread to avoid blocking the caller
        Task.Run(async () =>
        {
            Log(logAction, $"Starting process monitor for GameId: {gameId}");

            // Grace period
            if (options.GracePeriodMs > 0)
            {
                await Task.Delay(options.GracePeriodMs);
            }

            var startTime = DateTime.UtcNow;
            Process? targetProcess = null;

            // 1. Search Phase
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < options.MaxSearchTimeMs)
            {
                var processes = findProcesses();
                if (processes.Length > 0)
                {
                    targetProcess = processes[0];
                    Log(logAction, $"Process found: {targetProcess.ProcessName} (Id: {targetProcess.Id})");
                    break;
                }

                await Task.Delay(options.SearchIntervalMs);
            }

            if (targetProcess == null)
            {
                Log(logAction, "Process not found within timeout.");
                
                if (options.EnableLauncherHeuristic)
                {
                    Log(logAction, "Launcher Heuristic: Process never found. Switching to MANUAL tracking.");
                    // Don't stop session.
                    return;
                }

                // Default behavior: stop session if not found
                sessionManager.StopSession(gameId);
                return;
            }

            // 2. Monitor Phase (WaitForExit)
            var monitoringStartTime = DateTime.UtcNow;
            
            try
            {
                // We use WaitForExit() but on this background thread, so it blocks *this thread* only.
                // This is more efficient than a polling loop.
                if (!targetProcess.HasExited)
                {
                    targetProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Log(logAction, $"Error waiting for process exit: {ex.Message}");
                // Fallback to polling if WaitForExit fails permissions?
            }

            Log(logAction, $"Process {targetProcess.Id} exited.");

            // 3. Heuristic Check
            if (options.EnableLauncherHeuristic)
            {
                var duration = (DateTime.UtcNow - monitoringStartTime).TotalMilliseconds;
                if (duration < options.LauncherThresholdMs)
                {
                    Log(logAction, $"Process exited quickly ({duration}ms). Assuming launcher behavior. Switching to MANUAL tracking.");
                    return; // Don't stop session
                }
            }

            // Stop session
            sessionManager.StopSession(gameId);
        });
    }
}
