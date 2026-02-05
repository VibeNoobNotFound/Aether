using Aether.WinUI.Services;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;

using Serilog;
using System.IO;

namespace Aether.WinUI;

public partial class App : Application
{
    public Window? MainWindow { get; private set; }
    public IServiceProvider Services { get; private set; }
    public new static App Current => (App)Application.Current;

    public App()
    {
        // Setup Serilog
       var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Aether", "logs", "frontend");
        var logPath = Path.Combine(baseDir, "log-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Starting Aether.WinUI...");
        // Global Exception Handlers
        this.UnhandledException += App_UnhandledException;

        Services = ConfigureServices();
        InitializeComponent();
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled XAML Exception");
        // e.Handled = true; // Optional: try to keep app alive
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(logging => logging.AddSerilog());

        // Services
        services.AddSingleton<GrpcClientService>();
        services.AddSingleton<BackendManager>();
        services.AddSingleton<ImageCacheService>();
        services.AddSingleton<AppSettingsService>();
        services.AddSingleton<IconMapService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<SearchViewModel>();
        services.AddTransient<GameDetailViewModel>();

        return services.BuildServiceProvider();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            Log.Information("OnLaunched started");
            MainWindow = new MainWindow();
            MainWindow.Activate();

            MainWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            MainWindow.AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            // Start Backend
            var backend = Services.GetRequiredService<BackendManager>();
            await backend.StartAsync();
            Log.Information("Backend start requested");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Crash in OnLaunched");
            throw;
        }
    }
}
