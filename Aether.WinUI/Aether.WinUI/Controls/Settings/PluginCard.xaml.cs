using Aether.WinUI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aether.WinUI.Controls.Settings;

public sealed partial class PluginCard : UserControl
{
    private readonly ILogger<PluginCard> _logger;
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
        _logger = Ioc.Default.GetRequiredService<ILogger<PluginCard>>();
        _logger.LogDebug("PluginCard initialized");
    }

    private void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Plugin uninstall clicked");
        // TODO: Fire event to ViewModel to handle uninstall
    }
}
