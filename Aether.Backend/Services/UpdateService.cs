using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Aether.Backend.Services;

/// <summary>
/// Handles application updates using GitHub Releases via Octokit
/// </summary>
public class UpdateService
{
    private readonly GitHubClient _github;
    private const string Owner = "VibeNoobNotFound";
    private const string Repo = "Aether";
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
        _github = new GitHubClient(new ProductHeaderValue("Aether"));
    }

    public async Task<Protos.UpdateInfo> CheckForUpdates(string currentVersion, bool includePrerelease)
    {
        try
        {
            var releases = await _github.Repository.Release.GetAll(Owner, Repo);
            var platformSuffix = GetPlatformTagSuffix();

            // Filter releases by platform tag suffix (e.g., v1.1-macos, v1.0-linux)
            var latest = releases
                .Where(r => r.TagName.Contains(platformSuffix, StringComparison.OrdinalIgnoreCase))
                .Where(r => includePrerelease || !r.Prerelease)
                .FirstOrDefault();

            if (latest == null)
            {
                _logger.LogInformation("No releases found for platform suffix: {Suffix}", platformSuffix);
                return new Protos.UpdateInfo { UpdateAvailable = false };
            }

            // Extract version from tag: "v1.1-macos" -> "1.1"
            var newVersion = ExtractVersionFromTag(latest.TagName);
            if (!IsNewer(newVersion, currentVersion))
            {
                _logger.LogInformation("Current version {Current} is up to date (latest: {Latest})",
                    currentVersion, newVersion);
                return new Protos.UpdateInfo { UpdateAvailable = false };
            }

            var asset = GetPlatformAsset(latest.Assets);

            return new Protos.UpdateInfo
            {
                UpdateAvailable = true,
                Version = latest.TagName,
                ReleaseNotes = latest.Body ?? "",
                HtmlUrl = latest.HtmlUrl,
                DownloadUrl = asset?.BrowserDownloadUrl ?? "",
                IsPrerelease = latest.Prerelease,
                SizeBytes = asset?.Size ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            return new Protos.UpdateInfo { UpdateAvailable = false };
        }
    }

    /// <summary>
    /// Returns the platform suffix for tag filtering (e.g., "-macos", "-linux", "-windows")
    /// </summary>
    private string GetPlatformTagSuffix()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "-macos";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "-windows";
        else
            return "-linux";
    }

    /// <summary>
    /// Extracts version number from tag: "v1.1-macos" -> "1.1"
    /// </summary>
    private string ExtractVersionFromTag(string tag)
    {
        // Remove 'v' prefix and platform suffix: "v1.1-macos" -> "1.1"
        var platformtag = tag.Split(',').Where(x => x.Contains(GetPlatformTagSuffix())).FirstOrDefault();
        if (platformtag == null)
            return "0.0.0"; // Fallback to "0.0.0" if no platform suffix found

        var withoutPrefix = platformtag.TrimStart('v');
        var dashIndex = withoutPrefix.LastIndexOf('-');
        if (dashIndex > 0)
            return withoutPrefix[..dashIndex];

        return withoutPrefix;
    }

    public async IAsyncEnumerable<Protos.DownloadProgress> DownloadUpdate(string version)
    {
        var channel = System.Threading.Channels.Channel.CreateUnbounded<Protos.DownloadProgress>();

        // Start the download task
        _ = Task.Run(async () =>
        {
            try
            {
                await DownloadUpdateInternal(version, channel.Writer);
            }
            finally
            {
                channel.Writer.Complete();
            }
        });

        // Yield from the channel
        await foreach (var progress in channel.Reader.ReadAllAsync())
        {
            yield return progress;
        }
    }

    private async Task DownloadUpdateInternal(string version, System.Threading.Channels.ChannelWriter<Protos.DownloadProgress> writer)
    {
        // Get release
        IReadOnlyList<Release>? releases;
        try
        {
            releases = await _github.Repository.Release.GetAll(Owner, Repo);
        }
        catch (Exception ex)
        {
            await writer.WriteAsync(new Protos.DownloadProgress
            {
                Status = Protos.DownloadProgress.Types.Status.Failed,
                ErrorMessage = $"Failed to fetch releases: {ex.Message}"
            });
            return;
        }

        var release = releases?.FirstOrDefault(r => r.TagName == version);
        if (release == null)
        {
            await writer.WriteAsync(new Protos.DownloadProgress
            {
                Status = Protos.DownloadProgress.Types.Status.Failed,
                ErrorMessage = $"Release {version} not found"
            });
            return;
        }

        var asset = GetPlatformAsset(release.Assets);
        if (asset == null)
        {
            await writer.WriteAsync(new Protos.DownloadProgress
            {
                Status = Protos.DownloadProgress.Types.Status.Failed,
                ErrorMessage = "No compatible asset found for this platform"
            });
            return;
        }

        // Setup paths
        var tempDir = Path.Combine(Path.GetTempPath(), "aether-update");
        var archivePath = Path.Combine(tempDir, asset.Name);
        var extractPath = Path.Combine(tempDir, "extracted");

        try
        {
            Directory.CreateDirectory(tempDir);
            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);

            // Download with progress
            using var client = new HttpClient();
            using var response = await client.GetAsync(asset.BrowserDownloadUrl,
                HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? asset.Size;
            var buffer = new byte[8192];
            long downloaded = 0;
            int lastReportedPercent = -1;

            {
                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = File.Create(archivePath);

                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    downloaded += bytesRead;

                    var percent = totalBytes > 0 ? (int)(downloaded * 100 / totalBytes) : 0;

                    // Report every 5% to avoid flooding
                    if (percent != lastReportedPercent && percent % 5 == 0)
                    {
                        lastReportedPercent = percent;
                        await writer.WriteAsync(new Protos.DownloadProgress
                        {
                            Status = Protos.DownloadProgress.Types.Status.Downloading,
                            Percent = percent,
                            BytesDownloaded = downloaded,
                            TotalBytes = totalBytes
                        });
                    }
                }
            }

            // Extract
            await writer.WriteAsync(new Protos.DownloadProgress
            {
                Status = Protos.DownloadProgress.Types.Status.Extracting,
                Percent = 100
            });

            ZipFile.ExtractToDirectory(archivePath, extractPath, true);

            // Success
            await writer.WriteAsync(new Protos.DownloadProgress
            {
                Status = Protos.DownloadProgress.Types.Status.Complete,
                Percent = 100,
                ExtractPath = extractPath
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed");
            await writer.WriteAsync(new Protos.DownloadProgress
            {
                Status = Protos.DownloadProgress.Types.Status.Failed,
                ErrorMessage = ex.Message
            });
        }
    }

    public Protos.OperationStatus InstallUpdate(string extractPath)
    {
        try
        {
            var appPath = GetCurrentAppPath();
            var pid = Process.GetCurrentProcess().Id;

            // Generate helper script in temp location
            var helperPath = CreateHelperScript();

            _logger.LogInformation("Launching update helper: {Helper}", helperPath);
            _logger.LogInformation("Extract path: {Extract}, App path: {App}", extractPath, appPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = helperPath,
                Arguments = $"{pid} \"{extractPath}\" \"{appPath}\"",
                UseShellExecute = true,
                CreateNoWindow = false
            };

            // Make script executable on Unix
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("chmod", $"+x \"{helperPath}\"")?.WaitForExit();
            }

            Process.Start(startInfo);

            return new Protos.OperationStatus
            {
                Success = true,
                Message = "Update helper launched. Please restart the application."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch update helper");
            return new Protos.OperationStatus
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    private string CreateHelperScript()
    {
        string content;
        string extension;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            content = WindowsHelperScript;
            extension = ".bat";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            content = MacOsHelperScript;
            extension = ".sh";
        }
        else
        {
            content = LinuxHelperScript;
            extension = ".sh";
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "aether-update-helper");
        Directory.CreateDirectory(tempDir);
        var scriptPath = Path.Combine(tempDir, $"update_helper{extension}");

        File.WriteAllText(scriptPath, content);
        return scriptPath;
    }

    private ReleaseAsset? GetPlatformAsset(IReadOnlyList<ReleaseAsset> assets)
    {
        string pattern;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            pattern = "macos";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            pattern = "windows";
        else
            pattern = "linux";

        return assets.FirstOrDefault(a =>
            a.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase) &&
            a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
    }

    private bool IsNewer(string newVer, string currentVer)
    {
        try
        {
            return new Version(newVer).CompareTo(new Version(currentVer)) > 0;
        }
        catch
        {
            // Fallback to string comparison
            return string.Compare(newVer, currentVer, StringComparison.OrdinalIgnoreCase) > 0;
        }
    }

    private string GetCurrentAppPath()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exePath))
            throw new InvalidOperationException("Cannot determine current executable path");

        // On macOS, navigate up to .app bundle
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && exePath.Contains(".app"))
        {
            var appIndex = exePath.IndexOf(".app", StringComparison.Ordinal);
            return exePath[..(appIndex + 4)];
        }

        return Path.GetDirectoryName(exePath) ?? exePath;
    }

    private const string MacOsHelperScript = """
#!/bin/bash
# macOS Update Helper Script
# Arguments: PID NEW_PATH APP_PATH

LOG_FILE="/tmp/aether_update.log"
exec > >(tee -a "$LOG_FILE") 2>&1
echo "--- Update Started: $(date) ---"

PID=$1
NEW_PATH=$2
APP_PATH=$3

echo "Arguments:"
echo "PID: $PID"
echo "NEW_PATH: $NEW_PATH"
echo "APP_PATH: $APP_PATH"

echo "Waiting for process $PID to exit..."

# Wait for the app to exit
while kill -0 "$PID" 2>/dev/null; do
    sleep 0.5
done

echo "Process exited. preparing update..."

# Locate the .app inside the extraction directory
# This handles cases where the zip contains a top-level folder
FOUND_APP=$(find "$NEW_PATH" -maxdepth 2 -name "*.app" -type d | head -n 1)

if [ -z "$FOUND_APP" ]; then
echo "ERROR: Could not find any .app bundle in $NEW_PATH"
    exit 1
fi

echo "Found new app at: $FOUND_APP"

TARGET_DIR=$(dirname "$APP_PATH")

# Backup old app
if [ -d "$APP_PATH" ]; then
echo "Backing up old app to ${APP_PATH}.old"
    rm -rf "${APP_PATH}.old"
    mv "$APP_PATH" "${APP_PATH}.old"
fi

# Move new app to target location
echo "Moving $FOUND_APP to $APP_PATH"
# We act carefully: move the found app to the destination name
mv "$FOUND_APP" "$APP_PATH"

if [ $? -ne 0 ]; then
echo "ERROR: Failed to move app. Attempting restore..."
    mv "${APP_PATH}.old" "$APP_PATH"
    exit 1
fi

# Clean up temp files
echo "Cleaning up temp files..."
rm -rf "$(dirname "$NEW_PATH")"

# Clean up backup
echo "Removing backup..."
rm -rf "${APP_PATH}.old"

echo "Update complete. Relaunching..."

# Relaunch the app
open -n "$APP_PATH"
echo "--- Update Finished ---"
""";

    private const string LinuxHelperScript = """
#!/bin/bash
# Linux Update Helper Script
# Arguments: PID NEW_PATH APP_PATH

PID=$1
NEW_PATH=$2
APP_PATH=$3

echo "Aether Update Helper (Linux)"
echo "Waiting for process $PID to exit..."

# Wait for the app to exit
while kill -0 "$PID" 2>/dev/null; do
    sleep 0.5
done

echo "Process exited. Updating application..."

# Remove old files and copy new
rm -rf "$APP_PATH"/*
cp -R "$NEW_PATH/"* "$APP_PATH/"

# Make main executable executable
chmod +x "$APP_PATH/Aether" 2>/dev/null || chmod +x "$APP_PATH/AetherBackend" 2>/dev/null

# Clean up temp files
rm -rf "$(dirname "$NEW_PATH")"

echo "Update complete. Relaunching..."

# Relaunch
"$APP_PATH/Aether" &
""";

    private const string WindowsHelperScript = """
@echo off
REM Windows Update Helper Script
REM Arguments: PID NEW_PATH APP_PATH

set PID=%1
set NEW_PATH=%~2
set APP_PATH=%~3

echo Aether Update Helper (Windows)
echo Waiting for process %PID% to exit...

:wait
tasklist /FI "PID eq %PID%" 2>NUL | find "%PID%" >NUL
if not errorlevel 1 (
    timeout /t 1 /nobreak >NUL
    goto wait
)

echo Process exited. Updating application...

REM Remove old files and copy new
xcopy /E /Y /I "%NEW_PATH%\*" "%APP_PATH%\"

REM Clean up temp folder
rmdir /S /Q "%NEW_PATH%\.."

echo Update complete. Relaunching...

REM Relaunch
start "" "%APP_PATH%\Aether.exe"
""";
}
