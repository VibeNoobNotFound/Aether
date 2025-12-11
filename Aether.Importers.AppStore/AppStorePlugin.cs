using System.Diagnostics;
using Aether.PluginSDK;
using Aether.PluginSDK.Library;

namespace Aether.Importers.AppStore;

/// <summary>
/// macOS App Store and .app bundle importer
/// </summary>
public class AppStorePlugin : ILibraryImporter
{
    public string Name => "App Store";
    public string Version => "1.0.0";

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
}
