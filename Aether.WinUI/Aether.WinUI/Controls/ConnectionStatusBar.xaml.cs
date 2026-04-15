using Aether.WinUI.Services;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aether.WinUI.Controls;

public sealed partial class ConnectionStatusBar : UserControl
{
    public MainViewModel ViewModel => Ioc.Default.GetRequiredService<MainViewModel>();
    private BackendManager Backend => Ioc.Default.GetRequiredService<BackendManager>();
    private readonly ILogger<ConnectionStatusBar> _logger;

    public ConnectionStatusBar()
    {
        this.InitializeComponent();
        _logger = Ioc.Default.GetRequiredService<ILogger<ConnectionStatusBar>>();
        _logger.LogDebug("ConnectionStatusBar initialized");
    }

    private async void Retry_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Retry connection clicked");
        await Backend.RetryConnectionAsync();
    }
}
