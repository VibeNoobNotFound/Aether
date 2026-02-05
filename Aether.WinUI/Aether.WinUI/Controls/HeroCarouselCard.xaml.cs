using Aether.WinUI.Models;
using Aether.WinUI.Services;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Aether.WinUI.Controls;

public sealed partial class HeroCarouselCard : UserControl
{
    public GameViewModel Game { get { return (GameViewModel)GetValue(GameProperty); } set { SetValue(GameProperty, value); } }
    public static readonly DependencyProperty GameProperty = DependencyProperty.Register("Game", typeof(GameViewModel), typeof(HeroCarouselCard), new PropertyMetadata(null, OnGameChanged));
    private readonly ILogger<HeroCarouselCard> _logger;

    private static void OnGameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HeroCarouselCard card && e.NewValue is GameViewModel game)
        {
            card._logger.LogDebug("HeroCarouselCard game changed: {GameId}", game.Id);
        }
    }

    public MainViewModel ViewModel => Ioc.Default.GetRequiredService<MainViewModel>();

    public HeroCarouselCard()
    {
        this.InitializeComponent();
        _logger = Ioc.Default.GetRequiredService<ILogger<HeroCarouselCard>>();
        _logger.LogDebug("HeroCarouselCard initialized");
    }

    public Image HeroImageElement => HeroImage;

    private void HeroCarouselCard_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width <= 0 || e.NewSize.Height <= 0)
        {
            return;
        }

        var scale = Math.Clamp(e.NewSize.Width / 1000.0, 0.75, 1.15);
        var logoMax = Math.Clamp(e.NewSize.Height * 0.35, 120, 240);
        var titleSize = Math.Clamp(32 * scale, 22, 40);
        var playFont = Math.Clamp(24 * scale, 18, 28);
        var playPaddingH = Math.Clamp(24 * scale, 16, 30);
        var playPaddingV = Math.Clamp(12 * scale, 8, 16);
        var playRadius = Math.Clamp(18 * scale, 14, 24);
        var stopFont = Math.Clamp(14 * scale, 12, 18);
        var stopPaddingH = Math.Clamp(16 * scale, 12, 20);
        var stopPaddingV = Math.Clamp(8 * scale, 6, 12);
        var stopRadius = Math.Clamp(14 * scale, 10, 18);

        LogoImage.MaxHeight = logoMax;
        TitleText.FontSize = titleSize;
        PlayButton.FontSize = playFont;
        PlayButton.Padding = new Thickness(playPaddingH, playPaddingV, playPaddingH, playPaddingV);
        PlayButton.CornerRadius = new CornerRadius(playRadius);

        StopButton.FontSize = stopFont;
        StopButton.Padding = new Thickness(stopPaddingH, stopPaddingV, stopPaddingH, stopPaddingV);
        StopButton.CornerRadius = new CornerRadius(stopRadius);
    }
}
