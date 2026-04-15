using Aether.WinUI.Controls;
using Aether.WinUI.Services;
using Aether.WinUI.ViewModels;
using Aether.WinUI.Views.Insights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.System;

namespace Aether.WinUI.Views;

public sealed partial class HomePage : Page
{
    public MainViewModel ViewModel => Ioc.Default.GetRequiredService<MainViewModel>();
    private ImageCacheService ImageCache => Ioc.Default.GetRequiredService<ImageCacheService>();
    private readonly ILogger<HomePage> _logger;

    public HomePage()
    {
        this.InitializeComponent();
        this.Loaded += HomePage_Loaded;
        _logger = Ioc.Default.GetRequiredService<ILogger<HomePage>>();
        _logger.LogDebug("HomePage initialized");
    }

    private void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("HomePage loaded");
        // Load initial background from first carousel item
        UpdateBackgroundImage();
        _ = ViewModel.LoadGeneralNewsAsync();
    }

    private void HeroCarousel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _logger.LogDebug("HeroCarousel selection changed");
        UpdateBackgroundImage();
    }

    private async void UpdateBackgroundImage()
    {
        _logger.LogTrace("UpdateBackgroundImage invoked");
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
                    ViewModel.WindowBackgroundOpacity = 0.5;
                }
            }
            else
            {
                ViewModel.WindowBackgroundImageUrl = null;
                ViewModel.WindowBackgroundOpacity = 0.0;
            }
        }
    }

    private void HeroCarousel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _logger.LogTrace("HeroCarousel size changed");
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
        _logger.LogInformation("GridView item clicked");
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


    private async void NewsItem_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("News item clicked");
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
        _logger.LogInformation("EditCarousel clicked");
        var dialog = new Aether.WinUI.Views.Home.CarouselEditorDialog();
        dialog.XamlRoot = this.XamlRoot;
        await dialog.ShowAsync();
    }

    private async void ManageCollections_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("ManageCollections clicked");
        var dialog = new Aether.WinUI.Views.Library.CollectionManagerDialog();
        dialog.XamlRoot = this.XamlRoot;
        await dialog.ShowAsync();
    }

    private async void Insights_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Insights clicked");
        var dialog = new InsightsDialog();
        dialog.XamlRoot = this.XamlRoot;
        await dialog.ShowAsync();
    }

    private void Gamegcard_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Game grid card clicked");

        if (sender is GameGridCard card)
        {
            if (card.CoverImageElement != null)
            {
                Microsoft.UI.Xaml.Media.Animation.ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("CoverAnimation", card.CoverImageElement);
            }
            ViewModel.GoToGameDetail(card.Game.Id);
        }
    }
}
