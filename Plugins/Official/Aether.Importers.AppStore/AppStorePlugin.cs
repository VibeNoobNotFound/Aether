using System.Diagnostics;
using Aether.PluginSDK;
using Aether.PluginSDK.Library;

using Aether.PluginSDK.UI;

namespace Aether.Importers.AppStore;

/// <summary>
/// macOS App Store and .app bundle importer
/// </summary>
public class AppStorePlugin : ILibraryImporter, IGameLauncher
{
    public string Name => "App Store";
    public string Author => "VibeNoobNotFound";
    public string Version => "1.0.1";

    // App Store is only supported on MacOS
    public IEnumerable<string> SupportedPlatforms => new[] { "MacOS" };
    public bool SupportsManualAddition => false;

    public async Task<bool> CanImportAsync()
    {
        return OperatingSystem.IsMacOS();
    }

    public async IAsyncEnumerable<ImportedGame> ScanLibraryAsync(IProgress<ScanProgress>? progress = null)
    {
        if (!OperatingSystem.IsMacOS())
            yield break;

        var applicationsFolder = "/Applications";
        if (!Directory.Exists(applicationsFolder))
            yield break;

        var appBundles = Directory.GetDirectories(applicationsFolder, "*.app");
        int totalProcessed = 0;

        foreach (var appBundle in appBundles)
        {
            // Check if it's a game (heuristic: has certain categories or is known game)
            var game = await ParseAppBundleAsync(appBundle);
            if (game != null)
            {
                totalProcessed++;
                progress?.Report(new ScanProgress(
                    "App Store",
                    totalProcessed,
                    totalProcessed,
                    game.Title,
                    0
                ));

                yield return game;
            }
        }
    }

    private static async Task<ImportedGame?> ParseAppBundleAsync(string appBundlePath)
    {
        try
        {
            var infoPlistPath = Path.Combine(appBundlePath, "Contents", "Info.plist");
            if (!File.Exists(infoPlistPath))
                return null;

            // Use PlistBuddy to read app info
            var appName = await RunPlistBuddyAsync(infoPlistPath, "CFBundleDisplayName");
            if (string.IsNullOrEmpty(appName))
                appName = await RunPlistBuddyAsync(infoPlistPath, "CFBundleName");

            var bundleId = await RunPlistBuddyAsync(infoPlistPath, "CFBundleIdentifier");

            // CATEGORY CHECK: Ensure it's a game
            // LSApplicationCategoryType string usually looks like "public.app-category.games" or specific genres.
            var category = await RunPlistBuddyAsync(infoPlistPath, "LSApplicationCategoryType");

            // If category is found, check if it contains "game"
            // If category is null (some apps don't have it), we currently skip it to be safe, or we could include it if known game.
            // User requested strict "only games".
            bool isGame = !string.IsNullOrEmpty(category) && category.ToLower().Contains("game");

            if (!isGame)
                return null;

            if (string.IsNullOrEmpty(appName))
                appName = Path.GetFileNameWithoutExtension(appBundlePath);

            // Detect if it's from App Store (has receipt)
            var receiptPath = Path.Combine(appBundlePath, "Contents", "_MASReceipt", "receipt");
            var platform = File.Exists(receiptPath) ? "App Store" : "Custom";

            return new ImportedGame(
                appName,
                platform,
                bundleId ?? appName,
                appBundlePath,
                appBundlePath // The .app itself is executable
            );

        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> RunPlistBuddyAsync(string plistPath, string key)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/libexec/PlistBuddy",
                Arguments = $"-c \"Print :{key}\" \"{plistPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true, // Suppress "Entry Does Not Exist" errors
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output.Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    // IGameLauncher Implementation
    public bool CanLaunch(LaunchContext context)
    {
        // Can launch App Store games and Custom .app bundles on macOS
        return OperatingSystem.IsMacOS() &&
               (context.Platform == "App Store" || context.Platform == "Custom") &&
               !string.IsNullOrEmpty(context.InstallPath) &&
               context.InstallPath.EndsWith(".app");
    }

    public Task<LaunchResult> LaunchAsync(LaunchContext context)
    {
        var appPath = context.InstallPath;
        if (string.IsNullOrEmpty(appPath) || !Directory.Exists(appPath))
        {
            return Task.FromResult(LaunchResult.Failed($"App bundle not found: {appPath}"));
        }

        try
        {
            var startInfo = new ProcessStartInfo("open");
            startInfo.ArgumentList.Add("-a");
            startInfo.ArgumentList.Add(appPath);

            if (!string.IsNullOrEmpty(context.LaunchArguments))
            {
                startInfo.ArgumentList.Add("--args");
                // Split arguments to pass them correctly to open --args
                foreach (var arg in context.LaunchArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    startInfo.ArgumentList.Add(arg);
                }
            }

            startInfo.UseShellExecute = false;

            var process = Process.Start(startInfo);
            return Task.FromResult(LaunchResult.Succeeded(processId: process?.Id ?? 0, method: "macos_open"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(LaunchResult.Failed(ex.Message));
        }
    }

    public string? GetLaunchUri(string externalId)
    {
        // macOS apps don't have a protocol URI, just use open command
        return null;
    }

    public List<Widget> GetPluginWidgets(WidgetLocation location)
    {
        return new List<Widget>();
    }

    // IPlugin Implementation stubs
    public List<Widget> GetWidgets(Game game) => new List<Widget>();
    public Task<WidgetActionResult> OnWidgetAction(string actionId, string payload) => Task.FromResult(WidgetActionResult.Ok());
    public Task OnLibraryScan(LibraryContext context) => Task.CompletedTask;
    public Task OnGameLaunched(Game game) => Task.CompletedTask;
    public Task OnGameStopped(Game game, TimeSpan sessionDuration) => Task.CompletedTask;
}

