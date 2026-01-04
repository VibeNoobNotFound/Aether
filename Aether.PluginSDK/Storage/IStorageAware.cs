namespace Aether.PluginSDK.Storage;

/// <summary>
/// Interface for plugins that require persistent storage.
/// Implement this interface to receive storage injection from the backend.
/// </summary>
public interface IStorageAware
{
    /// <summary>
    /// Called by the backend to inject a storage instance for this plugin.
    /// </summary>
    void SetStorage(IPluginStorage storage);
}
