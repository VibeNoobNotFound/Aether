namespace Aether.PluginSDK.Library;

/// <summary>
/// Library importer interface for plugins that can detect and import games from various platforms
/// </summary>
public interface ILibraryImporter : IPlugin
{
    // Name is inherited from IPlugin
    string Version { get; }

    
    /// <summary>
    /// Check if this platform's launcher is installed on the system
    /// </summary>
    Task<bool> CanImportAsync();
    
    /// <summary>
    /// Scan the platform's library and yield discovered games
    /// </summary>
    IAsyncEnumerable<ImportedGame> ScanLibraryAsync(IProgress<ScanProgress>? progress = null);
}

public record ImportedGame(
    string Title,
    string Platform,
    string ExternalId, // Steam AppID, Epic GUID, etc.
    string InstallPath,
    string? ExecutablePath
);

public record ScanProgress(
    string CurrentPlatform,
    int GamesFound,
    int GamesProcessed,
    string? CurrentGame,
    double ProgressPercentage
);
