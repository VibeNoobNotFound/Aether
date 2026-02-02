using Aether.WinUI.Models;
using Aether.WinUI.Services;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Aether.WinUI.Views;

public sealed partial class GameDetailPage : Page
{
    public GameDetailViewModel ViewModel => (Application.Current as App)!.Services.GetRequiredService<GameDetailViewModel>();
    private ImageCacheService ImageCache => (Application.Current as App)!.Services.GetRequiredService<ImageCacheService>();

    public GameDetailPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is string gameId)
        {
            await ViewModel.LoadGameAsync(gameId);
            UpdateImages();
        }
    }

    private async void UpdateImages()
    {
        if (ViewModel.SelectedGame == null) return;

        if (ViewModel.SelectedGame.BackgroundImageUrl != null)
        {
            var bg = await ImageCache.GetImageAsync(ViewModel.SelectedGame.BackgroundImageUrl);
            if (bg != null) BackgroundImage.Source = bg;
        }

        if (ViewModel.SelectedGame.CoverImageUrl != null)
        {
            var cover = await ImageCache.GetImageAsync(ViewModel.SelectedGame.CoverImageUrl);
            if (cover != null) CoverImage.Source = cover;
        }
    }
}
