using Aether.WinUI.Services;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;

namespace Aether.WinUI;

public partial class App : Application
{
    private Window? _window;
    public IServiceProvider Services { get; private set; }
    public new static App Current => (App)Application.Current;

    public App()
    {
        Services = ConfigureServices();
        InitializeComponent();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Services
        services.AddSingleton<GrpcClientService>();
        services.AddSingleton<BackendManager>();
        services.AddSingleton<ImageCacheService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddTransient<GameDetailViewModel>();

        return services.BuildServiceProvider();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();

        // Start Backend
        var backend = Services.GetRequiredService<BackendManager>();
        await backend.StartAsync();
    }
}
