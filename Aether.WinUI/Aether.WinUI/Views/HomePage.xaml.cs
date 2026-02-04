using Aether.WinUI.ViewModels;
using Aether.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using Windows.Foundation;

namespace Aether.WinUI.Views;

public sealed partial class HomePage : Page
{
    public MainViewModel ViewModel => (Application.Current as App)!.Services.GetRequiredService<MainViewModel>();
    private ImageCacheService ImageCache => (Application.Current as App)!.Services.GetRequiredService<ImageCacheService>();

    public HomePage()
    {
        this.InitializeComponent();
        this.Loaded += HomePage_Loaded;
    }

    private void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        // Load initial background from first carousel item
        UpdateBackgroundImage();
        _ = ViewModel.LoadGeneralNewsAsync();
    }

    private void HeroCarousel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateBackgroundImage();
    }

    private async void UpdateBackgroundImage()
    {
        if (HeroCarousel.SelectedItem is Models.GameViewModel game)
        {
            var imageUrl = game.BackgroundImageUrl ?? game.CoverImageUrl;
            if (imageUrl != null)
            {
                var bitmap = await ImageCache.GetImageAsync(imageUrl);
                if (bitmap != null)
                {
                    BackgroundImage.Source = bitmap;
                    ViewModel.WindowBackgroundImageUrl = imageUrl;
                }
            }
            else
            {
                ViewModel.WindowBackgroundImageUrl = null;
            }
        }
    }

    private void HeroCarousel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Match macOS aspect ratio 460 / 215
        var ratio = 215.0 / 460.0;
        var newHeight = e.NewSize.Width * ratio;
        if (!double.IsNaN(newHeight) && newHeight > 0)
        {
            HeroCarousel.Height = newHeight;
            if (NewsPanel != null)
            {
                NewsPanel.Height = newHeight;
                NewsPanel.MaxHeight = newHeight;
            }
        }
    }

    private void GridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Models.GameViewModel game)
        {
            // Prepare Connected Animation
            if (sender is GridView gridView && gridView.ContainerFromItem(e.ClickedItem) is GridViewItem container)
            {
                if (container.ContentTemplateRoot is Controls.GameGridCard card && card.CoverImageElement != null)
                {
                    Microsoft.UI.Xaml.Media.Animation.ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("CoverAnimation", card.CoverImageElement);
                }
            }

            ViewModel.GoToGameDetail(game.Id);
        }
    }

    private void CollectionGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Models.GameViewModel game)
        {
            if (sender is ListView listView && listView.ContainerFromItem(e.ClickedItem) is ListViewItem container)
            {
                if (container.ContentTemplateRoot is Controls.GameGridCard card && card.CoverImageElement != null)
                {
                    Microsoft.UI.Xaml.Media.Animation.ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("CoverAnimation", card.CoverImageElement);
                }
            }

            ViewModel.GoToGameDetail(game.Id);
        }
    }

    private async void NewsItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string url && !string.IsNullOrWhiteSpace(url))
        {
            try
            {
                var uri = new Uri(url);
                await Launcher.LaunchUriAsync(uri);
            }
            catch
            {
                // Ignore invalid URLs
            }
        }
    }

    private async void EditCarousel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Aether.WinUI.Views.Home.CarouselEditorDialog();
        dialog.XamlRoot = this.XamlRoot;
        await dialog.ShowAsync();
    }

    private async void ManageCollections_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Aether.WinUI.Views.Library.CollectionManagerDialog();
        dialog.XamlRoot = this.XamlRoot;
        await dialog.ShowAsync();
    }

    private async void Insights_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Insights",
            Content = "Insights are coming soon. We’ll surface playtime, genre trends, and top games here.",
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
