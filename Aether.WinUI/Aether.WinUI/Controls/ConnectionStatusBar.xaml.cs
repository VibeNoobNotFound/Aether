using Aether.WinUI.Services;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aether.WinUI.Controls;

public sealed partial class ConnectionStatusBar : UserControl
{
    public MainViewModel ViewModel => (Application.Current as App)!.Services.GetRequiredService<MainViewModel>();
    private BackendManager Backend => (Application.Current as App)!.Services.GetRequiredService<BackendManager>();

    public ConnectionStatusBar()
    {
        this.InitializeComponent();
    }

    private async void Retry_Click(object sender, RoutedEventArgs e)
    {
        await Backend.RetryConnectionAsync();
    }
}
