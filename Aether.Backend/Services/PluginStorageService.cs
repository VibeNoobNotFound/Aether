using Aether.Backend.Data;
using Aether.PluginSDK.Storage;
using LiteDB;
using STJ = System.Text.Json;

namespace Aether.Backend.Services;

/// <summary>
/// LiteDB-backed implementation of IPluginStorage.
/// Each plugin gets its own isolated database file.
/// </summary>
public class PluginStorageService : IPluginStorage, IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<StorageEntry> _collection;
    private const string CollectionName = "storage";

    public PluginStorageService(string pluginName)
    {
       LibraryDatabase.GetDefaultDatabasePath(out var BaseDir);
       
        var dbPath = Path.Combine(BaseDir, $"{SanitizeFileName(pluginName)}.db");
        _db = new LiteDatabase($"Filename={dbPath};Connection=shared");
        _collection = _db.GetCollection<StorageEntry>(CollectionName);
        _collection.EnsureIndex(x => x.Key, unique: true);
    }

    public Task SaveAsync<T>(string key, T value)
    {
        var json = STJ.JsonSerializer.Serialize(value);
        var existing = _collection.FindOne(x => x.Key == key);

        var entry = existing ?? new StorageEntry { Key = key };
        entry.ValueJson = json;
        entry.TypeName = typeof(T).FullName ?? typeof(T).Name;

        _collection.Upsert(entry);
        return Task.CompletedTask;
    }

    public Task<T?> LoadAsync<T>(string key)
    {
        var entry = _collection.FindOne(x => x.Key == key);
        if (entry == null)
            return Task.FromResult<T?>(default);

        try
        {
            var value = STJ.JsonSerializer.Deserialize<T>(entry.ValueJson);
            return Task.FromResult(value);
        }
        catch
        {
            return Task.FromResult<T?>(default);
        }
    }

    public Task DeleteAsync(string key)
    {
        _collection.DeleteMany(x => x.Key == key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        var exists = _collection.Exists(x => x.Key == key);
        return Task.FromResult(exists);
    }

    public void Dispose()
    {
        _db?.Dispose();
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    private class StorageEntry
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();
        public string Key { get; set; } = "";
        public string ValueJson { get; set; } = "";
        public string TypeName { get; set; } = "";
    }
}
