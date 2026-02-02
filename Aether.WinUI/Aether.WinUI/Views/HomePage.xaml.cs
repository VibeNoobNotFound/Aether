using Aether.WinUI.ViewModels;
using Aether.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
                }
            }
        }
    }

    private void GridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Models.GameViewModel game)
        {
            ViewModel.GoToGameDetail(game.Id);
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
        var dialog = new ContentDialog
        {
            Title = "Manage Collections",
            Content = "Collection management coming soon.",
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
