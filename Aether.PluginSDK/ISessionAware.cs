namespace Aether.PluginSDK;

/// <summary>
/// Interface for plugins that require session management capabilities.
/// Implement this to receive session manager injection from the backend.
/// </summary>
public interface ISessionAware : IPlugin
{
    /// <summary>
    /// Called by the backend to inject the session manager instance.
    /// Store this reference to call StartSession/StopSession during game lifecycle.
    /// </summary>
    void SetSessionManager(ISessionManager sessionManager);
}
