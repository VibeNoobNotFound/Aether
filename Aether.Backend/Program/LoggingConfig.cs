using Serilog;
using Serilog.Events;
using System.Runtime.InteropServices;

namespace Aether.Backend;

public static class LoggingConfig
{
    public static void Initialize()
    {
        var logDir = GetLogDirectory();
        var mainLogFile = Path.Combine(logDir, "server.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            
            // Console output (all logs)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            
            // Main backend logs (exclude anything with PluginName property)
            .WriteTo.Logger(lc => lc
                .Filter.ByExcluding(e => e.Properties.ContainsKey("PluginName"))
                .WriteTo.File(mainLogFile, 
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"))
            
            // Plugin logs (route by PluginName to separate files)
            .WriteTo.Map(
                logEvent => GetPluginNameFromLogEvent(logEvent) ?? "unknown",
                (pluginName, wt) => 
                {
                    if (pluginName != "unknown")
                    {
                        var pluginLogDir = Path.Combine(logDir, "plugins", pluginName);
                        Directory.CreateDirectory(pluginLogDir);
                        var pluginLogFile = Path.Combine(pluginLogDir, $"{pluginName}.log");
                        
                        wt.File(pluginLogFile, 
                            rollingInterval: RollingInterval.Day,
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
                    }
                })
            .CreateLogger();
        
        LogStartupInfo();
    }

    private static string GetLogDirectory()
    {
        string baseDir;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            baseDir = Path.Combine(home, "Library/Application Support/Aether/logs/backend");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            baseDir = Path.Combine(home, ".local/share/Aether/logs/backend");
        }
        else // Windows
        {
            baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Aether", "logs", "backend");
        }
        
        Directory.CreateDirectory(baseDir);
        return baseDir;
    }

    private static void LogStartupInfo()
    {
        Log.Information("Backend starting...");
        Log.Information("HOME environment: {Home}", Environment.GetEnvironmentVariable("HOME"));
        Log.Information("UserProfile: {UserProfile}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        Log.Information("CurrentDirectory: {Cwd}", Environment.CurrentDirectory);
        Log.Information("BaseDirectory: {Base}", AppContext.BaseDirectory);
    }

    private static string? GetPluginNameFromLogEvent(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue("PluginName", out var pluginName))
        {
            return pluginName.ToString().Trim('"').ToLower();
        }
        return null;
    }
}
