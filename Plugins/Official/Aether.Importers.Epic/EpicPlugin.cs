using System.Diagnostics;
using System.Text.Json;
using Aether.PluginSDK;
using Aether.PluginSDK.Helpers;
using Aether.PluginSDK.Library;
using Aether.PluginSDK.UI;


namespace Aether.Importers.Epic;

/// <summary>
/// Epic Games Store library importer
/// </summary>
public class EpicPlugin : ILibraryImporter, IGameLauncher, ISessionAware, Aether.PluginSDK.Logging.ILoggingAware
{
    public string Name => "Epic Games";
    public string Author => "VibeNoobNotFound";
    public string Version => "1.1.0";

    public IEnumerable<string> SupportedPlatforms => Enumerable.Empty<string>(); // All platforms
    public bool SupportsManualAddition => false;

    // Logging
    private Serilog.ILogger? _logger;

    // Session Management
    private ISessionManager? _sessionManager;

    public void SetLogger(Serilog.ILogger logger)
    {
        _logger = logger;
        _logger.Information("EpicPlugin initialized");
    }

    public void SetSessionManager(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
        _logger?.Debug("Session manager injected");
    }

    public async Task<bool> CanImportAsync()
    {
        var manifestPaths = GetManifestPaths();
        return manifestPaths.Any(Directory.Exists);
    }

    public async IAsyncEnumerable<ImportedGame> ScanLibraryAsync(IProgress<ScanProgress>? progress = null)
    {
        _logger?.Information("Starting Epic Games scanning");
        var manifestPaths = GetManifestPaths();
        int totalProcessed = 0;

        foreach (var manifestPath in manifestPaths.Where(Directory.Exists))
        {
            _logger?.Debug("Scanning path: {Path}", manifestPath);
            IList<string> manifestFiles;
            try
            {
                manifestFiles = Directory.GetFiles(manifestPath, "*.item");
            }
            catch (Exception ex)
            {
                _logger?.Warning(ex, "Error scanning manifest path: {Path}", manifestPath);
                continue;
            }

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
        _logger?.Information("Epic Games scan complete. Found {Count} games.", totalProcessed);
    }

    private static List<string> GetManifestPaths()
    {
        var paths = new List<string>();

        if (OperatingSystem.IsMacOS())
        {
            // UserProfile may be empty when running in a sandboxed context
            // Use HOME environment variable as fallback
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home))
            {
                home = Environment.GetEnvironmentVariable("HOME") ?? "";
            }
            if (!string.IsNullOrEmpty(home))
            {
                paths.Add(Path.Combine(home, "Library", "Application Support", "Epic", "EpicGamesLauncher", "Data", "Manifests"));
            }
        }
        else if (OperatingSystem.IsWindows())
        {
            paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests"));
        }
        else if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home))
            {
                home = Environment.GetEnvironmentVariable("HOME") ?? "";
            }
            if (!string.IsNullOrEmpty(home))
            {
                paths.Add(Path.Combine(home, ".config", "Epic", "EpicGamesLauncher", "Data", "Manifests"));
            }
        }

        return paths;
    }

    private async Task<ImportedGame?> ParseManifestAsync(string manifestFile)
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
        catch (Exception ex)
        {
            _logger?.Warning(ex, "Failed to parse manifest: {Path}", manifestFile);
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
        _logger?.Information("Launching Epic game: {Name} ({Id})", context.Title, context.ExternalId);

        string? processName = null;
        if (!string.IsNullOrEmpty(context.ExecutablePath))
        {
            processName = Path.GetFileNameWithoutExtension(context.ExecutablePath);
        }

        // 1. Try Protocol Launch first (Preferred for reliability/DRM)
        var uri = GetLaunchUri(context.ExternalId);
        if (!string.IsNullOrEmpty(uri))
        {
            var result = LaunchHelper.LaunchUri(uri);

            if (result.Success)
            {
                // Start session tracking
                _sessionManager?.StartSession(context.GameId);

                // Start monitoring if we have a process name
                if (!string.IsNullOrEmpty(processName))
                {
                    _ = MonitorProcessAsync(context.GameId, processName);
                }
                else
                {
                    // No tracking possible, leave session for manual stop or timeout
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(10000);
                        // Session remains active for manual stop
                    });
                }

                // Return success with no backend tracking (we handle it)
                var finalResult = LaunchResult.Succeeded(processId: 0, method: "epic_protocol");
                finalResult.TrackingMethod = LaunchTrackingMethod.None;
                return Task.FromResult(finalResult);
            }
        }

        // 2. Fallback to Direct Launch
        if (!string.IsNullOrEmpty(context.ExecutablePath))
        {
            if (File.Exists(context.ExecutablePath) || Directory.Exists(context.ExecutablePath))
            {
                LaunchResult directResult;
                if (context.ExecutablePath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                {
                    directResult = LaunchHelper.LaunchMacOSApp(context.ExecutablePath);
                    processName = Path.GetFileNameWithoutExtension(context.ExecutablePath);
                }
                else
                {
                    directResult = LaunchHelper.LaunchExecutable(context.ExecutablePath, context.RunAsAdmin);
                }

                if (directResult.Success)
                {
                    _sessionManager?.StartSession(context.GameId);

                    if (directResult.ProcessId.HasValue)
                    {
                        _ = MonitorPidAsync(context.GameId, directResult.ProcessId.Value);
                    }
                    else if (!string.IsNullOrEmpty(processName))
                    {
                        _ = MonitorProcessAsync(context.GameId, processName);
                    }

                    directResult.TrackingMethod = LaunchTrackingMethod.None;
                }

                return Task.FromResult(directResult);
            }
            else
            {
                _logger?.Warning("Executable not found: {Path}", context.ExecutablePath);
            }
        }

        return Task.FromResult(LaunchResult.Failed("No launch method available for Epic game"));
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

    private async Task MonitorPidAsync(string gameId, int pid)
    {
        await ProcessMonitor.MonitorByIdAsync(
            gameId,
            pid,
            _sessionManager!,
            msg => _logger?.Debug(msg),
            new ProcessMonitorOptions { GracePeriodMs = 0 }
        );
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
    public List<Widget> GetPluginWidgets(WidgetLocation location) => new List<Widget>();
}
