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
    private GameDetailViewModel? _viewModel;
    public GameDetailViewModel ViewModel => _viewModel ??= (Application.Current as App)!.Services.GetRequiredService<GameDetailViewModel>();
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
            System.Diagnostics.Debug.WriteLine($"[GameDetailPage] Navigated to gameId: {gameId}");
            await ViewModel.LoadGameAsync(gameId);
            Bindings.Update(); // Force x:Bind to refresh after SelectedGame is set
            UpdateImages();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[GameDetailPage] Navigated without string parameter. Type: {e.Parameter?.GetType()}");
        }
    }

    private async void UpdateImages()
    {
        if (ViewModel.SelectedGame == null) return;

        try
        {
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load images: {ex.Message}");
        }
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedGame == null) return;

        var dialog = new Aether.WinUI.Views.Library.MetadataEditorDialog(ViewModel.SelectedGame);
        dialog.XamlRoot = this.XamlRoot;
        await dialog.ShowAsync();
    }
}
