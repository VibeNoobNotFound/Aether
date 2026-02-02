using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;

namespace Aether.WinUI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool isDarkTheme = true;
    [ObservableProperty] private string version = "1.0.0";
    
    // Add more settings as needed
    
    public void ToggleTheme()
    {
        if (App.Current.RequestedTheme == ApplicationTheme.Dark)
            App.Current.RequestedTheme = ApplicationTheme.Light;
        else
            App.Current.RequestedTheme = ApplicationTheme.Dark;
    }
}
