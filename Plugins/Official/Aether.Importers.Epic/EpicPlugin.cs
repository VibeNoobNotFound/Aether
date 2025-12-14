using System.Text.Json;
using Aether.PluginSDK;
using Aether.PluginSDK.Library;
using Aether.PluginSDK.UI;


namespace Aether.Importers.Epic;

/// <summary>
/// Epic Games Store library importer
/// </summary>
public class EpicPlugin : ILibraryImporter
{
    public string Name => "Epic Games";
    public string Version => "1.0.0";

    public async Task<bool> CanImportAsync()
    {
        var manifestPaths = GetManifestPaths();
        return manifestPaths.Any(Directory.Exists);
    }

    public async IAsyncEnumerable<ImportedGame> ScanLibraryAsync(IProgress<ScanProgress>? progress = null)
    {
        var manifestPaths = GetManifestPaths();
        int totalProcessed = 0;

        foreach (var manifestPath in manifestPaths.Where(Directory.Exists))
        {
            var manifestFiles = Directory.GetFiles(manifestPath, "*.item");

            foreach (var manifestFile in manifestFiles)
            {
                var game = await ParseManifestAsync(manifestFile);
                if (game != null)
                {
                    totalProcessed++;
                    progress?.Report(new ScanProgress(
                        "Epic Games",
                        totalProcessed,
                        totalProcessed,
                        game.Title,
                        0
                    ));

                    yield return game;
                }
            }
        }
    }

    private static List<string> GetManifestPaths()
    {
        var paths = new List<string>();

        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            paths.Add(Path.Combine(home, "Library", "Application Support", "Epic", "EpicGamesLauncher", "Data", "Manifests"));
        }
        else if (OperatingSystem.IsWindows())
        {
            paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests"));
        }
        else if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            paths.Add(Path.Combine(home, ".config", "Epic", "EpicGamesLauncher", "Data", "Manifests"));
        }

        return paths;
    }

    private static async Task<ImportedGame?> ParseManifestAsync(string manifestFile)
    {
        try
        {
            var json = await File.ReadAllTextAsync(manifestFile);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var displayName = root.GetProperty("DisplayName").GetString();
            var installLocation = root.GetProperty("InstallLocation").GetString();
            var appName = root.GetProperty("AppName").GetString();

            if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(installLocation))
                return null;

            return new ImportedGame(
                displayName,
                "Epic Games",
                appName ?? displayName,
                installLocation,
                null
            );
        }
        catch
        {
            return null;
        }
    }

    // IPlugin Implementation stubs

    public List<Widget> GetWidgets(Game game) => new List<Widget>();
    public Task OnWidgetAction(string actionId, string payload) => Task.CompletedTask;
    public Task OnLibraryScan(LibraryContext context) => Task.CompletedTask;
    public Task OnGameLaunched(Game game) => Task.CompletedTask;
    public Task OnGameStopped(Game game, TimeSpan sessionDuration) => Task.CompletedTask;
    public List<Widget> GetSetupWidgets() => new List<Widget>();
}

