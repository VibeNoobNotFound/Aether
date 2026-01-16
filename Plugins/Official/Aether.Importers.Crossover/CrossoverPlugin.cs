using System.Diagnostics;
using System.Runtime.InteropServices;
using Aether.PluginSDK;
using Aether.PluginSDK.Helpers;
using Aether.PluginSDK.Library;
using Aether.PluginSDK.UI;

namespace Aether.Importers.Crossover;

/// <summary>
/// CrossOver Importer for macOS and Linux
/// </summary>
public class CrossoverPlugin : ILibraryImporter, IGameLauncher, ISessionAware, Aether.PluginSDK.Logging.ILoggingAware
{
    public string Name => "CrossOver";
    public string Author => "VibeNoobNotFound";
    public string Version => "1.2.0";

    // Logging
    private Serilog.ILogger? _logger;

    // Session Management
    private ISessionManager? _sessionManager;

    public void SetLogger(Serilog.ILogger logger)
    {
        _logger = logger;
        _logger.Information("CrossoverPlugin initialized");
    }

    public void SetSessionManager(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
        _logger?.Debug("Session manager injected");
    }

    public IEnumerable<string> SupportedPlatforms => new[] { "MacOS", "Linux" };
    public bool SupportsManualAddition => false;

    public async Task<bool> CanImportAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var userApps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications", "CrossOver");
            return Directory.Exists(userApps);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var cxConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cxoffice");
            return Directory.Exists(cxConfig);
        }

        return false;
    }

    public async IAsyncEnumerable<ImportedGame> ScanLibraryAsync(IProgress<ScanProgress>? progress = null)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            await foreach (var game in ScanMacOsLibrary(progress))
            {
                yield return game;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            await foreach (var game in ScanLinuxLibrary(progress))
            {
                yield return game;
            }
        }
    }

    private async IAsyncEnumerable<ImportedGame> ScanMacOsLibrary(IProgress<ScanProgress>? progress)
    {
        var userApps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications", "CrossOver");
        if (!Directory.Exists(userApps))
        {
            _logger?.Warning("CrossOver applications folder not found at: {Path}", userApps);
            yield break;
        }

        // Recursive scan for .app bundles
        // Filter out apps that are inside other apps (e.g. Helper.app inside Game.app)
        var apps = Directory.GetDirectories(userApps, "*.app", SearchOption.AllDirectories)
            .Where(path =>
            {
                // check if the parent path contains .app
                var parent = Path.GetDirectoryName(path);
                while (parent != null && parent.StartsWith(userApps))
                {
                    if (parent.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                        return false;
                    parent = Path.GetDirectoryName(parent);
                }
                return true;
            })
            .ToArray();

        _logger?.Debug("Found {Count} CrossOver apps", apps.Length);
        int processed = 0;

        foreach (var app in apps)
        {
            processed++;
            var name = Path.GetFileNameWithoutExtension(app);

            progress?.Report(new ScanProgress(
                "CrossOver",
                apps.Length,
                processed,
                name,
                (double)processed / apps.Length * 100
            ));

            yield return new ImportedGame(
                Title: name,
                Platform: "CrossOver",
                ExternalId: name,
                InstallPath: app,
                ExecutablePath: app // macOS launches .app bundles directly
            );
        }
    }

    private async IAsyncEnumerable<ImportedGame> ScanLinuxLibrary(IProgress<ScanProgress>? progress)
    {
        // Robust method: Check .desktop files created by CrossOver
        var applicationsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "share", "applications");
        if (!Directory.Exists(applicationsDir))
        {
            _logger?.Warning("CrossOver applications folder not found at: {Path}", applicationsDir);
            yield break;
        }

        var desktopFiles = Directory.GetFiles(applicationsDir, "*.desktop", SearchOption.AllDirectories);
        _logger?.Debug("Found {Count} desktop files to scan", desktopFiles.Length);
        int processed = 0;

        foreach (var file in desktopFiles)
        {
            // Simple heuristic to check if it's a Crossover app
            // Usually invalid/complex, but often contain "crossover" or are symlinked from .cxoffice
            // Better: Check file content for "crossover" in Exec line or simple naming convention
            // Commonly: cxmenu-BottleName-AppName.desktop

            var content = await File.ReadAllTextAsync(file);
            if (content.Contains("crossover") || content.Contains("cxoffice"))
            {
                processed++;
                var name = Path.GetFileNameWithoutExtension(file);

                // Try to parse Name from .desktop
                var nameLine = content.Split('\n').FirstOrDefault(l => l.StartsWith("Name="));
                if (nameLine != null)
                {
                    name = nameLine.Substring(5).Trim();
                }

                progress?.Report(new ScanProgress(
                    "CrossOver",
                    desktopFiles.Length,
                    processed,
                    name,
                    0
                ));

                yield return new ImportedGame(
                    Title: name,
                    Platform: "CrossOver",
                    ExternalId: Path.GetFileName(file),
                    InstallPath: file,
                    ExecutablePath: file // Launch the .desktop file (or use gtk-launch / open)
                );
            }
        }
    }

    // IGameLauncher Implementation
    public bool CanLaunch(LaunchContext context)
    {
        return context.Platform == "CrossOver";
    }

    public async Task<LaunchResult> LaunchAsync(LaunchContext context)
    {
        _logger?.Information("Launching CrossOver game: {Name}", context.Title);
        try
        {
            var startInfo = new ProcessStartInfo();
            string? processName = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: open -a "Path/To/App.app" --args <launch_args>
                startInfo.FileName = "open";
                startInfo.ArgumentList.Add("-a");
                startInfo.ArgumentList.Add(context.ExecutablePath);

                if (!string.IsNullOrEmpty(context.LaunchArguments))
                {
                    startInfo.ArgumentList.Add("--args");
                    foreach (var arg in context.LaunchArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        startInfo.ArgumentList.Add(arg);
                    }
                }

                // For macOS .app bundles, track by bundle name
                if (context.InstallPath.EndsWith(".app"))
                {
                    processName = Path.GetFileNameWithoutExtension(context.InstallPath);
                }
            }
            else // Linux
            {
                // Linux: xdg-open for generic file handling
                startInfo.FileName = "xdg-open";
                startInfo.ArgumentList.Add(context.ExecutablePath);

                // For Linux .desktop files, try to extract app name
                if (context.ExecutablePath.EndsWith(".desktop"))
                {
                    processName = Path.GetFileNameWithoutExtension(context.ExecutablePath);
                }
            }

            startInfo.UseShellExecute = false;

            Process.Start(startInfo);

            // Start session tracking
            _sessionManager?.StartSession(context.GameId);

            // Start background monitoring if we have a process name
            if (!string.IsNullOrEmpty(processName))
            {
                _ = MonitorProcessAsync(context.GameId, processName);
            }
            else
            {
                // No process name found, so we can't track it automatically.
                // Leave the session running for manual stop.
                _logger?.Information("No reliable process name for tracking. Session will remain active until manually stopped.");
            }

            // Return success with no backend tracking (we handle it)
            var result = LaunchResult.Succeeded(processId: 0, method: "crossover");
            result.TrackingMethod = LaunchTrackingMethod.None;
            return result;
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Failed to launch CrossOver app: {Name}", context.Title);
            return LaunchResult.Failed($"Failed to launch CrossOver app: {ex.Message}");
        }
    }

    private async Task MonitorProcessAsync(string gameId, string processName)
    {
        // Use SDK Helper with partial matching and launcher heuristic enabled
        await ProcessMonitor.MonitorByPartialNameAsync(
            gameId,
            processName,
            _sessionManager!,
            msg => _logger?.Debug(msg),
            new ProcessMonitorOptions
            {
                GracePeriodMs = 4000,
                EnableLauncherHeuristic = true,
                LauncherThresholdMs = 15000,
                MaxSearchTimeMs = 30000
            }
        );
    }

    // IPlugin Stubs
    public List<Widget> GetWidgets(Game game) => new List<Widget>();
    public Task<WidgetActionResult> OnWidgetAction(string actionId, string payload) => Task.FromResult(WidgetActionResult.Ok());
    public Task OnLibraryScan(LibraryContext context) => Task.CompletedTask;
    public Task OnGameLaunched(Game game) => Task.CompletedTask;
    public Task OnGameStopped(Game game, TimeSpan sessionDuration) => Task.CompletedTask;
    public List<Widget> GetPluginWidgets(WidgetLocation location) => new List<Widget>();

    public string? GetLaunchUri(string externalId)
    {
        return null;
    }
}
