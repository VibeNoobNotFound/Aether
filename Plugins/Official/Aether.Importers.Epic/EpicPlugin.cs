using System.Text.Json;
using Aether.PluginSDK;
using Aether.PluginSDK.Library;
using Aether.PluginSDK.UI;


namespace Aether.Importers.Epic;

/// <summary>
/// Epic Games Store library importer
/// </summary>
public class EpicPlugin : ILibraryImporter, IGameLauncher
{
    public string Name => "Epic Games";
    public string Author => "VibeNoobNotFound";
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

    // IGameLauncher Implementation
    public bool CanLaunch(LaunchContext context)
    {
        return context.Platform == "Epic Games";
    }

    public Task<LaunchResult> LaunchAsync(LaunchContext context)
    {
        var uri = GetLaunchUri(context.ExternalId);
        if (string.IsNullOrEmpty(uri))
        {
            // Fallback: try direct executable launch
            if (!string.IsNullOrEmpty(context.ExecutablePath))
            {
                return Task.FromResult(LaunchHelper.LaunchExecutable(context.ExecutablePath, context.RunAsAdmin));
            }
            return Task.FromResult(LaunchResult.Failed("No launch method available for Epic game"));
        }

        return Task.FromResult(LaunchHelper.LaunchUri(uri));
    }

    public string? GetLaunchUri(string externalId)
    {
        if (string.IsNullOrEmpty(externalId))
            return null;
        // Epic format: com.epicgames.launcher://apps/{AppName}?action=launch&silent=true
        return $"com.epicgames.launcher://apps/{Uri.EscapeDataString(externalId)}?action=launch&silent=true";
    }

    // IPlugin Implementation stubs
    public List<Widget> GetWidgets(Game game) => new List<Widget>();
    public Task OnWidgetAction(string actionId, string payload) => Task.CompletedTask;
    public Task OnLibraryScan(LibraryContext context) => Task.CompletedTask;
    public Task OnGameLaunched(Game game) => Task.CompletedTask;
    public Task OnGameStopped(Game game, TimeSpan sessionDuration) => Task.CompletedTask;
    public List<Widget> GetSetupWidgets() => new List<Widget>();
}

