using Aether.WinUI.Models;
using Aether.WinUI.Services;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aether.WinUI.Controls;

public sealed partial class HeroCarouselCard : UserControl
{
    public GameViewModel Game { get { return (GameViewModel)GetValue(GameProperty); } set { SetValue(GameProperty, value); } }
    public static readonly DependencyProperty GameProperty = DependencyProperty.Register("Game", typeof(GameViewModel), typeof(HeroCarouselCard), new PropertyMetadata(null, OnGameChanged));

    private static void OnGameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HeroCarouselCard card)
        {
            card.ResetLogoState();
        }
    }

    private void ResetLogoState()
    {
        if (LogoImage != null) LogoImage.Visibility = Visibility.Collapsed;
        if (TitleText != null) TitleText.Visibility = Visibility.Visible;
    }

    private void LogoImage_ImageOpened(object sender, RoutedEventArgs e)
    {
        LogoImage.Visibility = Visibility.Visible;
        TitleText.Visibility = Visibility.Collapsed;
    }

    public MainViewModel ViewModel => (Application.Current as App)!.Services.GetRequiredService<MainViewModel>();

    public HeroCarouselCard()
    {
        this.InitializeComponent();
    }
}
