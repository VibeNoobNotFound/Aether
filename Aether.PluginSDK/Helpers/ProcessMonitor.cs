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

    /// <summary>
    /// Interval for fallback polling when event-based monitoring fails (ms).
    /// Default: 5000ms.
    /// </summary>
    public int FallbackPollingIntervalMs { get; set; } = 5000;
}

public static class ProcessMonitor
{
    private static void Log(Action<string>? logger, string message) => logger?.Invoke(message);

    /// <summary>
    /// Monitors a process by its ID.
    /// </summary>
    public static Task MonitorByIdAsync(
        string gameId,
        int processId,
        ISessionManager sessionManager,
        Action<string>? logAction = null,
        ProcessMonitorOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return MonitorInternalAsync(gameId, sessionManager, logAction, options, cancellationToken, () =>
        {
            try
            {
                var process = Process.GetProcessById(processId);
                return new[] { process };
            }
            catch (ArgumentException)
            {
                // Process doesn't exist
                return Array.Empty<Process>();
            }
            catch (Exception ex)
            {
                Log(logAction, $"Error getting process by ID {processId}: {ex.Message}");
                return Array.Empty<Process>();
            }
        });
    }

    /// <summary>
    /// Monitors a process by its name (exact match).
    /// </summary>
    public static Task MonitorByNameAsync(
        string gameId,
        string processName,
        ISessionManager sessionManager,
        Action<string>? logAction = null,
        ProcessMonitorOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            throw new ArgumentException("Process name cannot be null or whitespace.", nameof(processName));
        }

        return MonitorInternalAsync(gameId, sessionManager, logAction, options, cancellationToken, () =>
        {
            try
            {
                return Process.GetProcessesByName(processName);
            }
            catch (Exception ex)
            {
                Log(logAction, $"Error getting processes by name '{processName}': {ex.Message}");
                return Array.Empty<Process>();
            }
        });
    }

    /// <summary>
    /// Monitors a process by partial name match.
    /// </summary>
    public static Task MonitorByPartialNameAsync(
        string gameId,
        string processNameFragment,
        ISessionManager sessionManager,
        Action<string>? logAction = null,
        ProcessMonitorOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(processNameFragment))
        {
            throw new ArgumentException("Process name fragment cannot be null or whitespace.", nameof(processNameFragment));
        }

        return MonitorInternalAsync(gameId, sessionManager, logAction, options, cancellationToken, () =>
        {
            try
            {
                var allProcesses = Process.GetProcesses();
                var matchingProcesses = new List<Process>();

                foreach (var process in allProcesses)
                {
                    try
                    {
                        if (process.ProcessName.Contains(processNameFragment, StringComparison.OrdinalIgnoreCase))
                        {
                            matchingProcesses.Add(process);
                        }
                        else
                        {
                            // Dispose processes we don't need
                            process.Dispose();
                        }
                    }
                    catch
                    {
                        // Process may have exited or access denied
                        process.Dispose();
                    }
                }

                return matchingProcesses.ToArray();
            }
            catch (Exception ex)
            {
                Log(logAction, $"Error getting processes by partial name '{processNameFragment}': {ex.Message}");
                return Array.Empty<Process>();
            }
        });
    }

    private static async Task MonitorInternalAsync(
        string gameId,
        ISessionManager sessionManager,
        Action<string>? logAction,
        ProcessMonitorOptions? options,
        CancellationToken cancellationToken,
        Func<Process[]> findProcesses)
    {
        if (string.IsNullOrWhiteSpace(gameId))
        {
            throw new ArgumentException("Game ID cannot be null or whitespace.", nameof(gameId));
        }

        if (sessionManager == null)
        {
            throw new ArgumentNullException(nameof(sessionManager));
        }

        options ??= new ProcessMonitorOptions();

        try
        {
            Log(logAction, $"Starting process monitor for GameId: {gameId}");

            // Grace period
            if (options.GracePeriodMs > 0)
            {
                await Task.Delay(options.GracePeriodMs, cancellationToken);
            }

            var startTime = DateTime.UtcNow;
            Process[]? targetProcesses = null;

            // 1. Search Phase
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < options.MaxSearchTimeMs)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Log(logAction, "Process monitoring cancelled during search phase.");
                    return;
                }

                var processes = findProcesses();
                if (processes.Length > 0)
                {
                    targetProcesses = processes;
                    Log(logAction, $"Found {targetProcesses.Length} process(es). Monitoring all.");
                    break;
                }

                await Task.Delay(options.SearchIntervalMs, cancellationToken);
            }

            if (targetProcesses == null || targetProcesses.Length == 0)
            {
                Log(logAction, "Process not found within timeout.");

                if (options.EnableLauncherHeuristic)
                {
                    Log(logAction, "Launcher Heuristic: Process never found. Switching to MANUAL tracking.");
                    return;
                }

                sessionManager.StopSession(gameId);
                return;
            }

            // 2. Monitor Phase (WaitForExit for ALL processes)
            var monitoringStartTime = DateTime.UtcNow;

            try
            {
                await MonitorProcessesAsync(targetProcesses, logAction, options, cancellationToken);
            }
            finally
            {
                // Always dispose processes
                foreach (var process in targetProcesses)
                {
                    try
                    {
                        process?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log(logAction, $"Error disposing process: {ex.Message}");
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Log(logAction, "Process monitoring cancelled.");
                return;
            }

            Log(logAction, "All tracked processes have exited.");

            // 3. Heuristic Check
            if (options.EnableLauncherHeuristic)
            {
                var duration = (DateTime.UtcNow - monitoringStartTime).TotalMilliseconds;
                if (duration < options.LauncherThresholdMs)
                {
                    Log(logAction, $"Process(es) exited quickly ({duration:F0}ms). Assuming launcher behavior. Switching to MANUAL tracking.");
                    return;
                }
            }

            // Stop session
            sessionManager.StopSession(gameId);
            Log(logAction, $"Session stopped for GameId: {gameId}");
        }
        catch (OperationCanceledException)
        {
            Log(logAction, "Process monitoring was cancelled.");
        }
        catch (Exception ex)
        {
            Log(logAction, $"Unexpected error in process monitoring: {ex.Message}");
            // Optionally stop session on error
            try
            {
                sessionManager.StopSession(gameId);
            }
            catch (Exception stopEx)
            {
                Log(logAction, $"Error stopping session after monitoring failure: {stopEx.Message}");
            }
        }
    }

    /// <summary>
    /// Monitors multiple processes and waits for all to exit.
    /// </summary>
    private static async Task MonitorProcessesAsync(
        Process[] processes,
        Action<string>? logAction,
        ProcessMonitorOptions options,
        CancellationToken cancellationToken)
    {
        var exitTasks = new List<Task>();

        foreach (var process in processes)
        {
            exitTasks.Add(WaitForExitAsync(process, logAction, options, cancellationToken));
        }

        await Task.WhenAll(exitTasks);
    }

    /// <summary>
    /// Waits for process exit asynchronously without blocking a thread.
    /// Uses Exited event with proper cleanup and fallback to polling.
    /// </summary>
    private static async Task WaitForExitAsync(
        Process process,
        Action<string>? logAction,
        ProcessMonitorOptions options,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        EventHandler? exitHandler = null;

        try
        {
            // Check if already exited
            if (process.HasExited)
            {
                return;
            }

            process.EnableRaisingEvents = true;

            // Create handler with cleanup
            exitHandler = (sender, args) =>
            {
                tcs.TrySetResult(true);
            };

            process.Exited += exitHandler;

            // Double check in case it exited before we subscribed
            if (process.HasExited)
            {
                tcs.TrySetResult(true);
            }

            // Register cancellation
            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                await tcs.Task;
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited or can't enable events
            if (!process.HasExited)
            {
                Log(logAction, $"Cannot monitor process {process.Id} with events. Falling back to polling.");
                await PollForExitAsync(process, logAction, options, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log(logAction, $"Error setting up event monitoring for process {process.Id}: {ex.Message}. Falling back to polling.");
            await PollForExitAsync(process, logAction, options, cancellationToken);
        }
        finally
        {
            // Clean up event handler
            if (exitHandler != null)
            {
                try
                {
                    process.Exited -= exitHandler;
                }
                catch
                {
                    // Process may already be disposed
                }
            }
        }
    }

    /// <summary>
    /// Fallback polling method when event-based monitoring is not available.
    /// </summary>
    private static async Task PollForExitAsync(
        Process process,
        Action<string>? logAction,
        ProcessMonitorOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!process.HasExited)
            {
                await Task.Delay(options.FallbackPollingIntervalMs, cancellationToken);
            }
        }
        catch (InvalidOperationException)
        {
            // Process exited or access denied
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation
        }
        catch (Exception ex)
        {
            Log(logAction, $"Error during polling for process {process.Id}: {ex.Message}");
        }
    }
}