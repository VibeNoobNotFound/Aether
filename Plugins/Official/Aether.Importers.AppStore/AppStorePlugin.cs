using System.Diagnostics;
using Aether.PluginSDK;
using Aether.PluginSDK.Helpers;
using Aether.PluginSDK.Library;

using Aether.PluginSDK.UI;

namespace Aether.Importers.AppStore;

/// <summary>
/// macOS App Store and .app bundle importer
/// </summary>
public class AppStorePlugin : ILibraryImporter, IGameLauncher, ISessionAware, Aether.PluginSDK.Logging.ILoggingAware
{
    public string Name => "App Store";
    public string Author => "VibeNoobNotFound";
    public string Version => "1.1.0";

    // App Store is only supported on MacOS
    public IEnumerable<string> SupportedPlatforms => new[] { "MacOS" };
    public bool SupportsManualAddition => false;

    // Logging
    private Serilog.ILogger? _logger;

    // Session Management
    private ISessionManager? _sessionManager;

    public void SetLogger(Serilog.ILogger logger)
    {
        _logger = logger;
        _logger.Information("AppStorePlugin initialized");
    }

    public void SetSessionManager(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
        _logger?.Debug("Session manager injected");
    }

    public async Task<bool> CanImportAsync()
    {
        return OperatingSystem.IsMacOS();
    }

    public async IAsyncEnumerable<ImportedGame> ScanLibraryAsync(IProgress<ScanProgress>? progress = null)
    {
        if (!OperatingSystem.IsMacOS())
            yield break;

        _logger?.Information("Starting App Store scan");
        var applicationsFolder = "/Applications";
        if (!Directory.Exists(applicationsFolder))
        {
            _logger?.Warning("Applications folder not found at {Path}", applicationsFolder);
            yield break;
        }

        var appBundles = Directory.GetDirectories(applicationsFolder, "*.app");
        _logger?.Debug("Found {Count} app bundles in /Applications", appBundles.Length);

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
        _logger?.Information("App Store scan complete. Found {Count} games.", totalProcessed);
    }

    private async Task<ImportedGame?> ParseAppBundleAsync(string appBundlePath)
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
        catch (Exception ex)
        {
            _logger?.Warning(ex, "Failed to parse info.plist for {Path}", appBundlePath);
            return null;
        }
    }

    private async Task<string?> RunPlistBuddyAsync(string plistPath, string key)
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
        catch (Exception ex)
        {
            _logger?.Warning(ex, "Error running PlistBuddy for {Key} in {Path}", key, plistPath);
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
        _logger?.Information("Launching App Store game: {Name} ({InstallPath})", context.Title, context.InstallPath);
        var appPath = context.InstallPath;
        if (string.IsNullOrEmpty(appPath) || !Directory.Exists(appPath))
        {
            return Task.FromResult(LaunchResult.Failed($"App bundle not found: {appPath}"));
        }

        try
        {
            var startInfo = new ProcessStartInfo("/usr/bin/open");
            startInfo.WorkingDirectory = "/"; // Ensure CWD is valid
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

            Process.Start(startInfo);

            // Start session tracking via session manager
            var appName = Path.GetFileNameWithoutExtension(context.InstallPath);
            _sessionManager?.StartSession(context.GameId);

            // Start background monitoring for process exit
            _ = MonitorProcessAsync(context.GameId, appName);

            // Return success with no tracking (we handle it ourselves)
            var result = LaunchResult.Succeeded(processId: 0, method: "bundle");
            result.TrackingMethod = LaunchTrackingMethod.None;
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Failed to launch App Store game {Name}", context.Title);
            return Task.FromResult(LaunchResult.Failed(ex.Message));
        }
    }

    private async Task MonitorProcessAsync(string gameId, string processName)
    {
        await ProcessMonitor.MonitorByNameAsync(
            gameId,
            processName,
            _sessionManager!,
            msg => _logger?.Debug(msg),
            new ProcessMonitorOptions { GracePeriodMs = 3000 }
        );
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

