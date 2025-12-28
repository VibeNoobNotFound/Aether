using System.Diagnostics;

namespace Aether.PluginSDK;

/// <summary>
/// Interface for plugins that can launch games from their platform
/// </summary>
public interface IGameLauncher
{
    /// <summary>
    /// Check if this launcher can launch the specified game
    /// </summary>
    bool CanLaunch(LaunchContext context);

    /// <summary>
    /// Launch a game. Returns launch result with process info.
    /// </summary>
    Task<LaunchResult> LaunchAsync(LaunchContext context);

    /// <summary>
    /// Get the launch URI/command for external launching (optional)
    /// </summary>
    string? GetLaunchUri(string externalId);
}

/// <summary>
/// Context information for launching a game
/// </summary>
public class LaunchContext
{
    public string GameId { get; init; } = "";
    public string Title { get; init; } = "";
    public string Platform { get; init; } = "";
    public string ExternalId { get; init; } = "";
    public string InstallPath { get; init; } = "";
    public string ExecutablePath { get; init; } = "";
    public bool RunAsAdmin { get; init; }
    public string LaunchArguments { get; init; } = "";
}

/// <summary>
/// Result of a game launch attempt
/// </summary>
public class LaunchResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ProcessId { get; set; }
    public string? LaunchMethod { get; set; } // "protocol", "direct", "bundle"

    public static LaunchResult Succeeded(int? processId = null, string method = "direct")
        => new() { Success = true, ProcessId = processId, LaunchMethod = method };

    public static LaunchResult Failed(string error)
        => new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// Helper class for common launch operations
/// </summary>
public static class LaunchHelper
{
    /// <summary>
    /// Launch a URI using the system's default handler
    /// </summary>
    public static LaunchResult LaunchUri(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
            return LaunchResult.Succeeded(method: "protocol");
        }
        catch (Exception ex)
        {
            return LaunchResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Launch an executable directly
    /// </summary>
    public static LaunchResult LaunchExecutable(string executablePath, bool runAsAdmin = false, string? arguments = null)
    {
        if (string.IsNullOrEmpty(executablePath))
            return LaunchResult.Failed("Executable path is empty");

        if (!File.Exists(executablePath) && !Directory.Exists(executablePath))
            return LaunchResult.Failed($"Path not found: {executablePath}");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = Path.GetDirectoryName(executablePath),
                UseShellExecute = true,
                Arguments = arguments ?? ""
            };

            // Windows-specific admin elevation
            if (runAsAdmin && OperatingSystem.IsWindows())
            {
                psi.Verb = "runas";
            }

            var process = Process.Start(psi);
            return LaunchResult.Succeeded(process?.Id, "direct");
        }
        catch (Exception ex)
        {
            return LaunchResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Launch a macOS .app bundle
    /// </summary>
    public static LaunchResult LaunchMacOSApp(string appBundlePath)
    {
        if (!OperatingSystem.IsMacOS())
            return LaunchResult.Failed("macOS app bundles can only be launched on macOS");

        if (!Directory.Exists(appBundlePath))
            return LaunchResult.Failed($"App bundle not found: {appBundlePath}");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"-W -a \"{appBundlePath}\"", // -W waits for app to quit
                UseShellExecute = true
            };

            var process = Process.Start(psi);
            // open -W keeps the process alive, so we return "direct" method type to trigger tracking logic in LauncherService?
            // LauncherService checks for method == "direct". 
            // "bundle" is treated as method. 
            // I should either change this to "direct" OR update LauncherService to track "bundle" too.
            // Updating LauncherService to track "bundle" is cleaner.

            return LaunchResult.Succeeded(process?.Id, "bundle");
        }
        catch (Exception ex)
        {
            return LaunchResult.Failed(ex.Message);
        }
    }
}
