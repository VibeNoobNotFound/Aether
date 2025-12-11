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
    .WriteTo.File("logs/backend.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog(); // Use Serilog for all logging

    // Listen on 0.0.0.0:50051 for all platforms
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(50051, listenOptions =>
        {
            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
        });
    });
    Log.Information("Configured to listen on http://0.0.0.0:50051");

    // Add services to the container.
    builder.Services.AddGrpc();

    // Initialize database
    var dbPath = LibraryDatabase.GetDefaultDatabasePath();
    builder.Services.AddSingleton(sp =>
    {
        var logger = sp.GetRequiredService<Serilog.ILogger>();
        return new LibraryDatabase(dbPath, logger);
    });
    Log.Information("Database initialized at {Path}", dbPath);

    // Initialize plugin system
    var pluginPath = Path.Combine(AppContext.BaseDirectory, "plugins");
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
