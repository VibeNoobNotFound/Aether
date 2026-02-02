using Aether.WinUI.Models;
using Aether.WinUI.Services;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace Aether.WinUI.Controls;

public sealed partial class GameGridCard : UserControl
{
    public GameViewModel Game { get { return (GameViewModel)GetValue(GameProperty); } set { SetValue(GameProperty, value); } }
    public static readonly DependencyProperty GameProperty = DependencyProperty.Register("Game", typeof(GameViewModel), typeof(GameGridCard), new PropertyMetadata(null));

    public MainViewModel MainViewModel => (Application.Current as App)!.Services.GetRequiredService<MainViewModel>();
    private ImageCacheService ImageCache => (Application.Current as App)!.Services.GetRequiredService<ImageCacheService>();

    public GameGridCard()
    {
        this.InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (Game?.CoverImageUrl != null)
        {
            var bitmap = await ImageCache.GetImageAsync(Game.CoverImageUrl);
            if (bitmap != null)
            {
                CoverImage.Source = bitmap;
            }
        }
    }

    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string action && Game != null)
        {
            switch(action)
            {
                case "Launch":
                    _ = MainViewModel.LaunchGameCommand.ExecuteAsync(Game.Id);
                    break;
                case "Stop":
                    _ = MainViewModel.StopGameCommand.ExecuteAsync(Game.Id);
                    break;
                // TODO: Implement other actions
            }
        }
    }
}
