namespace Aether.PluginSDK;

/// <summary>
/// Interface for managing game sessions. Injected via ISessionAware.
/// Plugins can use this to start/stop sessions and update game state.
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Mark a game session as started. Increments PlayCount, sets LastPlayed.
    /// Call this when the game process has been successfully launched.
    /// </summary>
    void StartSession(string gameId);

    /// <summary>
    /// Mark a game session as stopped. Calculates duration and updates TotalPlaytime.
    /// Call this when the game process has exited.
    /// </summary>
    void StopSession(string gameId);

    /// <summary>
    /// Update the current state of a game session (for UI feedback).
    /// Use this to show "Launching" state before the game fully starts.
    /// </summary>
    void SetState(string gameId, SessionState state);

    /// <summary>
    /// Check if a game session is currently active.
    /// </summary>
    bool IsSessionActive(string gameId);
}

/// <summary>
/// Represents the state of a game session for UI display.
/// </summary>
public enum SessionState
{
    Stopped = 0,
    Launching = 1,
    Running = 2
}
