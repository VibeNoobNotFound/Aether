namespace Aether.PluginSDK.Storage;

/// <summary>
/// Provides isolated, persistent key-value storage for plugins.
/// Each plugin receives its own storage instance, isolated from other plugins.
/// </summary>
public interface IPluginStorage
{
    /// <summary>
    /// Saves a value with the specified key. Overwrites if key exists.
    /// </summary>
    Task SaveAsync<T>(string key, T value);

    /// <summary>
    /// Loads a value by key. Returns null/default if not found.
    /// </summary>
    Task<T?> LoadAsync<T>(string key);

    /// <summary>
    /// Deletes a value by key.
    /// </summary>
    Task DeleteAsync(string key);

    /// <summary>
    /// Checks if a key exists in storage.
    /// </summary>
    Task<bool> ExistsAsync(string key);
}
