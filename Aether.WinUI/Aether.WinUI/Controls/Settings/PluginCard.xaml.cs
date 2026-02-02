using Aether.WinUI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aether.WinUI.Controls.Settings;

public sealed partial class PluginCard : UserControl
{
    public PluginViewModel Plugin
    {
        get => (PluginViewModel)GetValue(PluginProperty);
        set => SetValue(PluginProperty, value);
    }
    public static readonly DependencyProperty PluginProperty =
        DependencyProperty.Register("Plugin", typeof(PluginViewModel), typeof(PluginCard), new PropertyMetadata(null));

    public PluginCard()
    {
        this.InitializeComponent();
    }

    private void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Fire event to ViewModel to handle uninstall
    }
}
