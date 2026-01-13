using System.Diagnostics;
using System.Runtime.InteropServices;
using Aether.PluginSDK;
using Aether.PluginSDK.Library;
using Aether.PluginSDK.UI;

namespace Aether.Importers.Crossover;

/// <summary>
/// CrossOver Importer for macOS and Linux
/// </summary>
public class CrossoverPlugin : ILibraryImporter, IGameLauncher, Aether.PluginSDK.Logging.ILoggingAware
{
    public string Name => "CrossOver";
    public string Author => "VibeNoobNotFound";
    public string Version => "1.0.3";

    // Logging
    private Serilog.ILogger? _logger;

    public void SetLogger(Serilog.ILogger logger)
    {
        _logger = logger;
        _logger.Information("CrossoverPlugin initialized");
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: open -a "Path/To/App.app" --args <launch_args>
                startInfo.FileName = "open";
                startInfo.ArgumentList.Add("-a");
                startInfo.ArgumentList.Add(context.ExecutablePath); // Currently InstallPath == ExecutablePath for apps

                if (!string.IsNullOrEmpty(context.LaunchArguments))
                {
                    startInfo.ArgumentList.Add("--args");
                    // Split args if necessary, or pass as single string if open supports it properly
                    // Simpler: Just pass the string
                    foreach (var arg in context.LaunchArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        startInfo.ArgumentList.Add(arg);
                    }
                }
            }
            else // Linux
            {
                // Linux: gtk-launch or direct execution of .desktop
                // Executing .desktop files directly needs specific handling (gtk-launch `basename` or parsing Exec line)
                // For simplicity, we assume we can execute it if it's marked executable, or fallback to parsing

                // Heuristic: Use 'gtk-launch' which usually works with .desktop names (without path/extension)
                // BUT context.ExecutablePath is full path.

                // Let's try xdg-open for generic "open this file" handling
                startInfo.FileName = "xdg-open";
                startInfo.ArgumentList.Add(context.ExecutablePath);

                // xdg-open generally doesn't accept extra args for the target app easily
                // For robust Linux launch with args, we'd need to parse the 'Exec' line from the .desktop file.
                // Keeping it simple for V1.
            }

            startInfo.UseShellExecute = false;

            var process = Process.Start(startInfo);
            if (process != null)
            {
                return LaunchResult.Succeeded(processId: process.Id, method: "direct");
            }

            // Fire and forget (open command returns immediately)
            return LaunchResult.Succeeded(processId: 0, method: "direct");
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Failed to launch CrossOver app: {Name}", context.Title);
            return LaunchResult.Failed($"Failed to launch CrossOver app: {ex.Message}");
        }
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
