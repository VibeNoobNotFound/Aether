using Aether.WinUI.Models;
using Aether.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aether.WinUI.Controls;

public sealed partial class HeroCarouselCard : UserControl
{
    public GameViewModel Game { get { return (GameViewModel)GetValue(GameProperty); } set { SetValue(GameProperty, value); } }
    public static readonly DependencyProperty GameProperty = DependencyProperty.Register("Game", typeof(GameViewModel), typeof(HeroCarouselCard), new PropertyMetadata(null));

    private ImageCacheService ImageCache => (Application.Current as App)!.Services.GetRequiredService<ImageCacheService>();

    public HeroCarouselCard()
    {
        this.InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (Game?.BackgroundImageUrl != null)
        {
            var bitmap = await ImageCache.GetImageAsync(Game.BackgroundImageUrl);
            if (bitmap != null)
            {
                HeroImage.Source = bitmap;
            }
        }
    }
}
