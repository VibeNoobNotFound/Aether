using System.Collections.Concurrent;
using System.Diagnostics;
using Aether.Backend.Data;
using Aether.PluginSDK;
using Aether.Protos;
using Microsoft.Extensions.Logging;

namespace Aether.Backend.Services;

/// <summary>
/// Manages game session lifecycle, tracking playtime, and broadcasting state changes.
/// Implements ISessionManager for plugin injection.
/// </summary>
public class GameSessionManager : ISessionManager
{
    private readonly ILogger<GameSessionManager> _logger;
    private readonly LibraryDatabase _database;
    private readonly ConcurrentDictionary<int, GameSession> _activeSessions = new();

    // Event for status updates (subscribed by gRPC)
    public event Action<int, GameState>? OnGameStateChanged;

    public IEnumerable<int> GetActiveGameIds() => _activeSessions.Keys;

    public GameSessionManager(ILogger<GameSessionManager> logger, LibraryDatabase database)
    {
        _logger = logger;
        _database = database;

        // Start monitoring loop (fallback for plugins that don't call StopSession)
        _ = MonitorSessionsAsync();
    }

    #region ISessionManager Implementation (for plugins)

    /// <summary>
    /// Start a session for a game. Called by plugins implementing ISessionAware.
    /// </summary>
    public void StartSession(string gameId)
    {
        if (!int.TryParse(gameId, out int id))
        {
            _logger.LogWarning("Invalid game ID format: {GameId}", gameId);
            return;
        }

        StartSessionInternal(id, new LaunchResult { TrackingMethod = LaunchTrackingMethod.None });
    }

    /// <summary>
    /// Stop a session for a game. Called by plugins implementing ISessionAware.
    /// </summary>
    public void StopSession(string gameId)
    {
        if (!int.TryParse(gameId, out int id))
        {
            _logger.LogWarning("Invalid game ID format: {GameId}", gameId);
            return;
        }

        StopSessionInternal(id, killProcess: false);
    }

    /// <summary>
    /// Update the state of a game session. Called by plugins for UI feedback.
    /// </summary>
    public void SetState(string gameId, SessionState state)
    {
        if (!int.TryParse(gameId, out int id))
        {
            _logger.LogWarning("Invalid game ID format: {GameId}", gameId);
            return;
        }

        var protoState = state switch
        {
            SessionState.Running => GameState.Running,
            SessionState.Launching => GameState.Launching,
            _ => GameState.Stopped
        };

        NotifyStateChange(id, protoState);
    }

    /// <summary>
    /// Check if a game session is currently active.
    /// </summary>
    public bool IsSessionActive(string gameId)
    {
        return int.TryParse(gameId, out int id) && _activeSessions.ContainsKey(id);
    }

    /// <summary>
    /// Add a process to be tracked for this session.
    /// Called by ProcessMonitor when it discovers game processes.
    /// </summary>
    public void AddTrackedProcess(string gameId, TrackedProcess process)
    {
        if (!int.TryParse(gameId, out int id))
        {
            _logger.LogWarning("[GameSessionManager] Invalid game ID format for AddTrackedProcess: {GameId}", gameId);
            return;
        }

        if (_activeSessions.TryGetValue(id, out var session))
        {
            // Avoid duplicates
            lock (session.TrackedProcesses)
            {
                if (!session.TrackedProcesses.Any(p => p.ProcessId == process.ProcessId))
                {
                    session.TrackedProcesses.Add(process);
                    _logger.LogInformation("[GameSessionManager] Added tracked process: {Process} for game {GameId}",
                        process, id);
                }
            }
        }
        else
        {
            _logger.LogWarning("[GameSessionManager] No active session found for game {GameId} when adding process", id);
        }
    }

    /// <summary>
    /// Get the cancellation token for a session's process monitoring.
    /// When the session is stopped, this token will be cancelled.
    /// </summary>
    public CancellationToken GetSessionCancellationToken(string gameId)
    {
        if (!int.TryParse(gameId, out int id))
        {
            _logger.LogWarning("[GameSessionManager] Invalid game ID format for GetSessionCancellationToken: {GameId}", gameId);
            return CancellationToken.None;
        }

        if (_activeSessions.TryGetValue(id, out var session))
        {
            return session.MonitorCts.Token;
        }

        _logger.LogWarning("[GameSessionManager] No active session found for game {GameId} when getting cancellation token", id);
        return CancellationToken.None;
    }

    #endregion

    #region Backend Methods (for gRPC service)

    /// <summary>
    /// Start a session with tracking info from LaunchResult. Called by gRPC service.
    /// </summary>
    public void StartSession(int gameId, LaunchResult result)
    {
        StartSessionInternal(gameId, result);
    }

    /// <summary>
    /// Stop a session and optionally kill the process. Called by gRPC service (StopGame).
    /// </summary>
    public void StopSession(int gameId)
    {
        StopSessionInternal(gameId, killProcess: true);
    }

    /// <summary>
    /// Get all tracked processes for an active game session.
    /// Called by gRPC service (GetActiveProcesses) for frontend confirmation dialog.
    /// </summary>
    public IReadOnlyList<TrackedProcess> GetTrackedProcesses(int gameId)
    {
        if (_activeSessions.TryGetValue(gameId, out var session))
        {
            lock (session.TrackedProcesses)
            {
                return session.TrackedProcesses.ToList().AsReadOnly();
            }
        }
        _logger.LogDebug("[GameSessionManager] No active session found for game {GameId} when getting tracked processes", gameId);
        return Array.Empty<TrackedProcess>();
    }

    #endregion

    #region Internal Implementation

    private void StartSessionInternal(int gameId, LaunchResult result)
    {
        if (_activeSessions.ContainsKey(gameId))
        {
            _logger.LogWarning("[GameSessionManager] Session already active for game {Id}", gameId);
            return;
        }

        var session = new GameSession
        {
            GameId = gameId,
            StartTime = DateTime.UtcNow,
            TrackingMethod = result.TrackingMethod,
            TrackingTarget = result.TrackingTarget,
            ManagedByPlugin = result.TrackingMethod == LaunchTrackingMethod.None
        };

        // If we have a process ID from launch result, add it as initial tracked process
        if (result.ProcessId.HasValue && result.ProcessId.Value > 0)
        {
            session.TrackedProcesses.Add(new TrackedProcess
            {
                ProcessId = result.ProcessId.Value,
                ProcessName = result.TrackingTarget,
                ExecutablePath = null
            });
            _logger.LogDebug("[GameSessionManager] Added initial tracked process {Pid} from launch result", result.ProcessId.Value);
        }

        if (_activeSessions.TryAdd(gameId, session))
        {
            _logger.LogInformation("[GameSessionManager] Started session for game {Id} (Method: {Method}, Target: {Target}, PluginManaged: {Managed})",
                gameId, session.TrackingMethod, session.TrackingTarget, session.ManagedByPlugin);

            // Increment Play Count immediately
            _database.UpdatePlayCount(gameId);
            _database.UpdateLastPlayed(gameId, DateTime.UtcNow);

            // LOG SESSION START
            session.DbSessionId = _database.LogSessionStart(gameId, session.StartTime);

            NotifyStateChange(gameId, GameState.Running);
        }
    }

    private void StopSessionInternal(int gameId, bool killProcess)
    {
        if (_activeSessions.TryRemove(gameId, out var session))
        {
            _logger.LogInformation("[GameSessionManager] Stopping session for game {Id} (killProcess: {Kill}, trackedProcesses: {Count})",
                gameId, killProcess, session.TrackedProcesses.Count);

            // Cancel the monitoring task first
            try
            {
                session.MonitorCts.Cancel();
                _logger.LogDebug("[GameSessionManager] Cancelled monitoring task for game {Id}", gameId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[GameSessionManager] Error cancelling monitoring: {Message}", ex.Message);
            }

            FinalizeSession(session);

            // Kill all tracked processes if requested
            if (killProcess && session.TrackedProcesses.Count > 0)
            {
                _logger.LogInformation("[GameSessionManager] Killing {Count} tracked process(es) for game {Id}",
                    session.TrackedProcesses.Count, gameId);

                foreach (var tracked in session.TrackedProcesses)
                {
                    try
                    {
                        var process = Process.GetProcessById(tracked.ProcessId);
                        if (!process.HasExited)
                        {
                            process.Kill();
                            _logger.LogInformation("[GameSessionManager] Killed process {Pid} ({Name}) for game {Id}",
                                tracked.ProcessId, tracked.ProcessName ?? "unknown", gameId);
                        }
                        else
                        {
                            _logger.LogDebug("[GameSessionManager] Process {Pid} already exited", tracked.ProcessId);
                        }
                    }
                    catch (ArgumentException)
                    {
                        _logger.LogDebug("[GameSessionManager] Process {Pid} no longer exists", tracked.ProcessId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("[GameSessionManager] Failed to kill process {Pid}: {Message}",
                            tracked.ProcessId, ex.Message);
                    }
                }
            }

            // Dispose the CancellationTokenSource
            session.MonitorCts.Dispose();
        }
        else
        {
            _logger.LogDebug("[GameSessionManager] No active session found for game {Id} to stop", gameId);
        }
    }

    private async Task MonitorSessionsAsync()
    {
        while (true)
        {
            try
            {
                foreach (var session in _activeSessions.Values.ToList())
                {
                    // Skip sessions managed by plugins (they call StopSession themselves)
                    if (session.ManagedByPlugin)
                        continue;

                    if (CheckSessionEnded(session))
                    {
                        if (_activeSessions.TryRemove(session.GameId, out _))
                        {
                            FinalizeSession(session);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in session monitoring loop");
            }

            await Task.Delay(2000); // Check every 2 seconds
        }
    }

    private bool CheckSessionEnded(GameSession session)
    {
        switch (session.TrackingMethod)
        {
            case LaunchTrackingMethod.Pid:
                // Check if any tracked processes are still running
                if (session.TrackedProcesses.Count > 0)
                {
                    foreach (var tracked in session.TrackedProcesses)
                    {
                        try
                        {
                            var process = Process.GetProcessById(tracked.ProcessId);
                            if (!process.HasExited)
                            {
                                return false; // At least one process still running
                            }
                        }
                        catch (ArgumentException)
                        {
                            // Process ID not found = exited, continue checking others
                        }
                    }
                    return true; // All tracked processes have exited
                }
                return true; // No tracked processes = ended

            case LaunchTrackingMethod.ProcessName:
                if (!string.IsNullOrEmpty(session.TrackingTarget))
                {
                    var processes = Process.GetProcessesByName(session.TrackingTarget);

                    // Grace period of 15 seconds for process to spawn
                    if (DateTime.UtcNow - session.StartTime < TimeSpan.FromSeconds(15) && processes.Length == 0)
                    {
                        return false; // Grace period
                    }

                    return processes.Length == 0;
                }
                return true; // Invalid target

            case LaunchTrackingMethod.None:
                // Plugin-managed sessions should not reach here (ManagedByPlugin = true)
                // For legacy/fallback, end after short delay
                return DateTime.UtcNow - session.StartTime > TimeSpan.FromSeconds(5);

            default:
                return true;
        }
    }

    private void FinalizeSession(GameSession session)
    {
        var duration = DateTime.UtcNow - session.StartTime;
        _logger.LogInformation("Session ended for game {Id}. Duration: {Duration}", session.GameId, duration);

        // Update DB stats (only if tracked for > 30 sec)
        if (duration.TotalSeconds > 30)
            if (duration.TotalSeconds > 30)
            {
                _database.UpdatePlaytime(session.GameId, duration);
            }

        // LOG SESSION END
        if (session.DbSessionId > 0)
        {
            _database.LogSessionEnd(session.DbSessionId, DateTime.UtcNow);
        }

        NotifyStateChange(session.GameId, GameState.Stopped);
    }

    private void NotifyStateChange(int gameId, GameState state)
    {
        OnGameStateChanged?.Invoke(gameId, state);
    }

    #endregion

    private class GameSession
    {
        public int GameId { get; set; }
        public DateTime StartTime { get; set; }
        public List<TrackedProcess> TrackedProcesses { get; set; } = new();
        public CancellationTokenSource MonitorCts { get; set; } = new();
        public LaunchTrackingMethod TrackingMethod { get; set; }
        public string? TrackingTarget { get; set; }

        public bool ManagedByPlugin { get; set; }
        public int DbSessionId { get; set; } // TRACK THE DB ID
    }
}

