using Aether.WinUI.Services;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private ILogger<App>? _logger;

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
        Ioc.Default.ConfigureServices(Services);
        _logger = Ioc.Default.GetRequiredService<ILogger<App>>();
        _logger.LogInformation("App services configured");
        InitializeComponent();
        _logger.LogInformation("App initialized");
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unhandled XAML exception");
        Log.Fatal(e.Exception, "Unhandled XAML Exception");
        // e.Handled = true; // Optional: try to keep app alive
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        var serilogger = Log.Logger ?? new LoggerConfiguration().CreateLogger();
        services.AddLogging(logging => logging.AddSerilog(serilogger, dispose: false));

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
            _logger?.LogInformation("OnLaunched started");
            MainWindow = new MainWindow();
            MainWindow.Activate();

            MainWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            MainWindow.AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            // Start Backend
            var backend = Ioc.Default.GetRequiredService<BackendManager>();
            _logger?.LogInformation("Starting backend");
            await backend.StartAsync();
            Log.Information("Backend start requested");
            _logger?.LogInformation("Backend start requested");

            var settings = Ioc.Default.GetRequiredService<AppSettingsService>();
            if (!settings.HasCompletedOnboarding)
            {
                _logger?.LogInformation("Showing onboarding dialog");
                var dialog = new Views.Onboarding.OnboardingDialog();
                if (MainWindow.Content is FrameworkElement root)
                {
                    dialog.XamlRoot = root.XamlRoot;
                }
                await dialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "Crash in OnLaunched");
            Log.Fatal(ex, "Crash in OnLaunched");
            throw;
        }
    }
}
