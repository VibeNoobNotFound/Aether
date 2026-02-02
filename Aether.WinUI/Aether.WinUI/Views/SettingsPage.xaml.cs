using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aether.WinUI.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel => (Application.Current as App)!.Services.GetRequiredService<SettingsViewModel>();

    public SettingsPage()
    {
        this.InitializeComponent();
    }
}
