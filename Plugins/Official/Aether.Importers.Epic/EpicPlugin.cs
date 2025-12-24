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

            string? launchExecutable = null;
            if (root.TryGetProperty("LaunchExecutable", out var exeProp))
                launchExecutable = exeProp.GetString();

            if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(installLocation))
                return null;

            string? fullExePath = null;
            if (!string.IsNullOrEmpty(launchExecutable))
            {
                fullExePath = Path.Combine(installLocation, launchExecutable);
            }

            return new ImportedGame(
                displayName,
                "Epic Games",
                appName ?? displayName,
                installLocation,
                fullExePath
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
        // 1. Try Protocol Launch first (Preferred for reliability/DRM)
        var uri = GetLaunchUri(context.ExternalId);
        if (!string.IsNullOrEmpty(uri))
        {
            return Task.FromResult(LaunchHelper.LaunchUri(uri));
        }

        // 2. Fallback to Direct Launch (Enables Playtime Tracking if successful)
        if (!string.IsNullOrEmpty(context.ExecutablePath))
        {
            // Verify file existence (weak check on macOS app bundles, but LaunchHelper handles logic)
            if (File.Exists(context.ExecutablePath) || Directory.Exists(context.ExecutablePath))
            {
                // For macOS .app bundles inside Epic games (rare but possible), or just binaries
                if (context.ExecutablePath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(LaunchHelper.LaunchMacOSApp(context.ExecutablePath));

                return Task.FromResult(LaunchHelper.LaunchExecutable(context.ExecutablePath, context.RunAsAdmin));
            }
        }

        return Task.FromResult(LaunchResult.Failed("No launch method available for Epic game"));
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
    public Task<WidgetActionResult> OnWidgetAction(string actionId, string payload) => Task.FromResult(WidgetActionResult.Ok());
    public Task OnLibraryScan(LibraryContext context) => Task.CompletedTask;
    public Task OnGameLaunched(Game game) => Task.CompletedTask;
    public Task OnGameStopped(Game game, TimeSpan sessionDuration) => Task.CompletedTask;
    public List<Widget> GetSetupWidgets() => new List<Widget>();
}

