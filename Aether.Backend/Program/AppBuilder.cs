using Aether.Backend.Services;
using Aether.Backend.Plugins;
using Aether.Backend.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Aether.Backend;

public static class AppBuilder
{
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureKestrel(builder);
        ConfigureServices(builder.Services);

        var app = builder.Build();

        // Eagerly load PluginManager to ensure plugins are loaded on startup
        using (var scope = app.Services.CreateScope())
        {
            _ = scope.ServiceProvider.GetRequiredService<PluginManager>();
        }

        ConfigurePipeline(app);

        return app;
    }

    private static void ConfigureKestrel(WebApplicationBuilder builder)
    {
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
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Add gRPC
        services.AddGrpc();
        services.AddSingleton<Serilog.ILogger>(Log.Logger);

        // Initialize database
        var dbPath = LibraryDatabase.GetDefaultDatabasePath(out var Basedir); ;
        services.AddSingleton<LibraryDatabase>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LibraryDatabase>>();
            var db = new LibraryDatabase(dbPath, logger);
            // Seeding deferred until plugins are loaded
            return db;
        });
        Log.Information("Database initialized at {Path}", dbPath);

        // Add GameSessionManager
        services.AddSingleton<GameSessionManager>();

        // Initialize plugin system
        var pluginPath = GetPluginPath();
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<Serilog.ILogger>();
            var manager = new PluginManager(pluginPath, logger);
            manager.LoadPlugins();

            // Inject session manager into plugins that support it
            var sessionManager = sp.GetRequiredService<GameSessionManager>();
            manager.SetSessionManager(sessionManager);

            // Seed database with plugins now available
            var db = sp.GetRequiredService<LibraryDatabase>();
            db.SeedDefaultCollections(manager.GetLibraryImporters());

            return manager;
        });
        Log.Information("Plugin system initialized at {Path}", pluginPath);

        // Initialize update service
        services.AddSingleton<UpdateService>();
        Log.Information("Update service initialized");
    }

    private static string GetPluginPath()
    {
        // Priority: 1. PLUGINS_PATH env var (from Swift app), 2. plugins subfolder, 3. base directory (for Contents/MacOS bundling)
        var envPluginPath = Environment.GetEnvironmentVariable("PLUGINS_PATH");

        if (!string.IsNullOrEmpty(envPluginPath) && Directory.Exists(envPluginPath))
        {
            Log.Information("Using PLUGINS_PATH environment variable: {Path}", envPluginPath);
            return envPluginPath;
        }

        var pluginsSubfolder = Path.Combine(AppContext.BaseDirectory, "plugins");
        if (Directory.Exists(pluginsSubfolder))
        {
            return pluginsSubfolder;
        }

        // Fallback: plugins are in the same directory as the executable (macOS Contents/MacOS bundling)
        return AppContext.BaseDirectory;
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        // Configure the HTTP request pipeline
        app.MapGrpcService<AetherGrpcService>();

        app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");
    }
}
