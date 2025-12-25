using Aether.Backend.Services;
using Aether.Backend.Plugins;
using Aether.Backend.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;

// Initialize Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/server.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Debug: Log important paths for troubleshooting sandbox issues
Log.Information("Backend starting...");
Log.Information("HOME environment: {Home}", Environment.GetEnvironmentVariable("HOME"));
Log.Information("UserProfile: {UserProfile}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
Log.Information("CurrentDirectory: {Cwd}", Environment.CurrentDirectory);
Log.Information("BaseDirectory: {Base}", AppContext.BaseDirectory);

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog(); // Use Serilog for all logging

    // Listen on 0.0.0.0:55551 for all platforms
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(55551, listenOptions =>
        {
            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
        });
    });
    Log.Information("Configured to listen on http://0.0.0.0:55551");

    // Add services to the container.
    builder.Services.AddGrpc();
    builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger);


    // Initialize database
    var dbPath = LibraryDatabase.GetDefaultDatabasePath();
    builder.Services.AddSingleton(sp =>
    {
        var logger = sp.GetRequiredService<Serilog.ILogger>();
        return new LibraryDatabase(dbPath, logger);
    });
    Log.Information("Database initialized at {Path}", dbPath);

    // Initialize plugin system
    // Priority: 1. PLUGINS_PATH env var (from Swift app), 2. plugins subfolder, 3. base directory (for Contents/MacOS bundling)
    var envPluginPath = Environment.GetEnvironmentVariable("PLUGINS_PATH");
    string pluginPath;
    if (!string.IsNullOrEmpty(envPluginPath) && Directory.Exists(envPluginPath))
    {
        pluginPath = envPluginPath;
        Log.Information("Using PLUGINS_PATH environment variable: {Path}", pluginPath);
    }
    else
    {
        var pluginsSubfolder = Path.Combine(AppContext.BaseDirectory, "plugins");
        if (Directory.Exists(pluginsSubfolder))
        {
            pluginPath = pluginsSubfolder;
        }
        else
        {
            // Fallback: plugins are in the same directory as the executable (macOS Contents/MacOS bundling)
            pluginPath = AppContext.BaseDirectory;
        }
    }
    builder.Services.AddSingleton(sp =>
    {
        var logger = sp.GetRequiredService<Serilog.ILogger>();
        var manager = new PluginManager(pluginPath, logger);
        manager.LoadPlugins();
        return manager;
    });
    Log.Information("Plugin system initialized at {Path}", pluginPath);

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.MapGrpcService<AetherGrpcService>();
    app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
