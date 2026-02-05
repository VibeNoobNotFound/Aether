using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aether.WinUI.Controls.Settings;

public sealed partial class SettingsActionCard : UserControl
{
    private readonly ILogger<SettingsActionCard> _logger;
    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register("Header", typeof(string), typeof(SettingsActionCard), new PropertyMetadata(string.Empty));

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register("Description", typeof(string), typeof(SettingsActionCard), new PropertyMetadata(string.Empty));

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register("IconGlyph", typeof(string), typeof(SettingsActionCard), new PropertyMetadata("\uE700")); // Default icon

    public event RoutedEventHandler? Click;

    public SettingsActionCard()
    {
        this.InitializeComponent();
        _logger = Ioc.Default.GetRequiredService<ILogger<SettingsActionCard>>();
        _logger.LogDebug("SettingsActionCard initialized");
    }

    private void SettingsCard_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("SettingsActionCard clicked: {Header}", Header);
        Click?.Invoke(this, e);
    }
}
