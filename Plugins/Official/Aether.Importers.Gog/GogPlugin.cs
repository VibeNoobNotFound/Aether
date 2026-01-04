using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Aether.PluginSDK;
using Aether.PluginSDK.Library;
using Aether.PluginSDK.UI;

namespace Aether.Importers.Gog;

public class GogPlugin : ILibraryImporter, IGameLauncher, Aether.PluginSDK.Logging.ILoggingAware
{
    public string Name => "GOG";
    public string Author => "VibeNoobNotFound";
    public string Version => "1.0.1";

    public IEnumerable<string> SupportedPlatforms => new[] { "Windows", "MacOS", "Linux" };
    public bool SupportsManualAddition => false;

    public async Task<bool> CanImportAsync()
    {
        var scanPaths = GetScanPaths();
        return scanPaths.Any(Directory.Exists);
    }

    // Logging
    private Serilog.ILogger? _logger;

    public void SetLogger(Serilog.ILogger logger)
    {
        _logger = logger;
        _logger.Information("GogPlugin initialized");
    }

    public async IAsyncEnumerable<ImportedGame> ScanLibraryAsync(IProgress<ScanProgress>? progress = null)
    {
        _logger?.Information("Starting GOG library scan");
        var scanPaths = GetScanPaths();
        int totalFiles = 0; // Estimation usually hard, we'll track processed count

        foreach (var path in scanPaths)
        {
            if (!Directory.Exists(path))
            {
                _logger?.Debug("Scan path not found: {Path}", path);
                continue;
            }

            _logger?.Debug("Scanning path: {Path}", path);

            // macOS: GOG games are often .app bundles. The metadata might be inside.
            // We need to look inside standard recursed paths, AND inside .app/Contents/Resources

            var options = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true, MaxRecursionDepth = 4 };

            // Standard scan
            IEnumerable<string> allFiles;
            try
            {
                var infoFiles = Directory.EnumerateFiles(path, "goggame-*.info", options);
                var jsonFiles = Directory.EnumerateFiles(path, "goggame-*.json", options);
                allFiles = infoFiles.Concat(jsonFiles).ToList();
            }
            catch (Exception ex)
            {
                _logger?.Warning(ex, "Error enumerating files in {Path}", path);
                continue;
            }

            // Special macOS check: Scan inside top-level .app bundles in the search directory
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    var appBundles = Directory.EnumerateDirectories(path, "*.app", SearchOption.TopDirectoryOnly);
                    foreach (var app in appBundles)
                    {
                        var resourcesPath = Path.Combine(app, "Contents", "Resources");
                        if (Directory.Exists(resourcesPath))
                        {
                            var innerInfo = Directory.EnumerateFiles(resourcesPath, "goggame-*.info", SearchOption.TopDirectoryOnly);
                            var innerJson = Directory.EnumerateFiles(resourcesPath, "goggame-*.json", SearchOption.TopDirectoryOnly);
                            ((List<string>)allFiles).AddRange(innerInfo);
                            ((List<string>)allFiles).AddRange(innerJson);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Warning(ex, "Error scanning app bundles in {Path}", path);
                }
            }

            foreach (var file in allFiles.Distinct())
            {
                var game = await ParseGogInfo(file);
                if (game != null)
                {
                    totalFiles++;
                    progress?.Report(new ScanProgress("GOG", 0, totalFiles, game.Title, 0));
                    yield return game;
                }
            }
        }
        _logger?.Information("GOG scan complete. Found {Count} games.", totalFiles);
    }

    private IEnumerable<string> GetScanPaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new[]
            {
                @"C:\GOG Games",
                @"C:\Program Files (x86)\GOG Galaxy\Games",
                @"C:\Program Files\GOG Galaxy\Games"
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new[]
            {
                "/Applications",
                Path.Combine(home, "Applications"),
                Path.Combine(home, "GOG Games"),
                "/Users/Shared/GOG"
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new[]
            {
                Path.Combine(home, "GOG Games"),
                Path.Combine(home, "Games", "GOG")
            };
        }

        return Array.Empty<string>();
    }

    private async Task<ImportedGame?> ParseGogInfo(string infoPath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(infoPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("gameId", out var idProp)) return null;
            var gameId = idProp.GetString();

            string title = "";
            if (root.TryGetProperty("name", out var nameProp)) title = nameProp.GetString() ?? "";
            else if (root.TryGetProperty("rootGameId", out var rootNameProp)) title = rootNameProp.GetString() ?? ""; // Fallback

            if (string.IsNullOrEmpty(gameId) || string.IsNullOrEmpty(title)) return null;

            // Determine Executable
            var installDir = Path.GetDirectoryName(infoPath) ?? "";
            string executablePath = DetectExecutable(installDir, root);

            return new ImportedGame(
                Title: title,
                Platform: "GOG",
                ExternalId: gameId,
                InstallPath: installDir,
                ExecutablePath: executablePath // Can be empty if detection failed, user can set manually later
            );
        }
        catch (Exception ex)
        {
            _logger?.Warning(ex, "Error parsing GOG info file: {Path}", infoPath);
            return null;
        }
    }

    private string DetectExecutable(string installDir, JsonElement root)
    {
        // 1. Try "playTasks" in JSON (Windows/Linux structure)
        if (root.TryGetProperty("playTasks", out var tasks) && tasks.ValueKind == JsonValueKind.Array)
        {
            foreach (var task in tasks.EnumerateArray())
            {
                if (task.TryGetProperty("isPrimary", out var isPrimary) && isPrimary.GetBoolean())
                {
                    if (task.TryGetProperty("path", out var path))
                    {
                        var fullPath = Path.Combine(installDir, path.GetString() ?? "");
                        if (File.Exists(fullPath)) return fullPath;
                    }
                }
            }
        }

        // 2. Heuristics based on OS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Largest EXE or matching name
            // ... (keep existing)
            var exes = Directory.GetFiles(installDir, "*.exe");
            if (exes.Any())
            {
                return exes.OrderByDescending(f => new FileInfo(f).Length).First();
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // If info file was found inside Game.app/Contents/Resources/goggame.info, 
            // then 'installDir' is .../Game.app/Contents/Resources.
            // We want to return .../Game.app

            if (installDir.Contains(".app"))
            {
                var segments = installDir.Split(Path.DirectorySeparatorChar);
                string builtPath = "";
                foreach (var seg in segments)
                {
                    if (string.IsNullOrEmpty(builtPath) && Path.IsPathRooted(installDir)) builtPath = "/"; // fix absolute path start handling
                    builtPath = Path.Combine(builtPath, seg);
                    if (seg.EndsWith(".app"))
                    {
                        return builtPath;
                    }
                }
            }

            // Fallback: Detect .app bundle in directory
            if (installDir.EndsWith(".app")) return installDir;

            var apps = Directory.GetDirectories(installDir, "*.app");
            if (apps.Any()) return apps.First();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Look for start.sh
            var startSh = Path.Combine(installDir, "start.sh");
            if (File.Exists(startSh)) return startSh;
        }

        return installDir; // Fallback to directory
    }

    // --- IGameLauncher Implementation ---

    public bool CanLaunch(LaunchContext context)
    {
        return context.Platform == "GOG";
    }

    public async Task<LaunchResult> LaunchAsync(LaunchContext context)
    {
        _logger?.Information("Launching GOG game: {Title} ({Id})", context.Title, context.ExternalId);
        string path = context.ExecutablePath;
        if (string.IsNullOrEmpty(path))
        {
            // Fallback to InstallPath heuristic if exe missing
            // But usually context should be populated
            return LaunchResult.Failed("Executable path is missing");
        }

        try
        {
            var startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true; // Default

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                startInfo.FileName = path;
                startInfo.WorkingDirectory = Path.GetDirectoryName(path);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Handle .app bundles
                if (path.EndsWith(".app"))
                {
                    startInfo.FileName = "open";
                    startInfo.ArgumentList.Add("-n"); // New instance
                    startInfo.ArgumentList.Add("-a"); // Application
                    startInfo.ArgumentList.Add(path);
                    if (!string.IsNullOrEmpty(context.LaunchArguments))
                    {
                        startInfo.ArgumentList.Add("--args");
                        startInfo.ArgumentList.Add(context.LaunchArguments);
                    }
                }
                else
                {
                    // Raw binary/script on mac? 
                    startInfo.FileName = path;
                    startInfo.UseShellExecute = false; // For scripts
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: often a script "start.sh" or a binary
                startInfo.FileName = path;
                startInfo.WorkingDirectory = Path.GetDirectoryName(path);

                // Ensure executable permission?
                // Assuming script/binary is executable.

                // If it's a shell script, might need "bash"
                if (path.EndsWith(".sh"))
                {
                    // Usually works directly if shebang exists
                }
            }

            var process = Process.Start(startInfo);
            return LaunchResult.Succeeded(process?.Id, "direct");
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Failed to launch GOG game: {Title}", context.Title);
            return LaunchResult.Failed($"Failed to launch GOG game: {ex.Message}");
        }
    }

    public string? GetLaunchUri(string externalId)
    {
        // GOG Galaxy Protocol: goggalaxy://openGameView/{id}
        // But we are focusing on DRM-free / Direct launch. 
        // Returning null forces Aether to use LaunchAsync.
        return null;
    }

    // --- IPlugin Stubs ---
    public List<Widget> GetWidgets(Game game) => new List<Widget>();
    public Task<WidgetActionResult> OnWidgetAction(string actionId, string payload) => Task.FromResult(WidgetActionResult.Ok());
    public Task OnLibraryScan(LibraryContext context) => Task.CompletedTask;
    public Task OnGameLaunched(Game game) => Task.CompletedTask;
    public Task OnGameStopped(Game game, TimeSpan sessionDuration) => Task.CompletedTask;
    public List<Widget> GetPluginWidgets(WidgetLocation location) => new List<Widget>();
}
