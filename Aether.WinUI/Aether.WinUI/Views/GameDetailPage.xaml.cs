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
        this.Loaded += GameDetailPage_Loaded;
    }

    private void GameDetailPage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateFavoriteButton();
    }

    private void UpdateFavoriteButton()
    {
        if (ViewModel?.SelectedGame == null || FavoriteIcon == null || FavoriteText == null) return;

        if (ViewModel.SelectedGame.IsFavorite)
        {
            FavoriteIcon.Glyph = "\uE735"; // Filled star
            FavoriteText.Text = "Unfavorite";
        }
        else
        {
            FavoriteIcon.Glyph = "\uE734"; // Outline star
            FavoriteText.Text = "Favorite";
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string gameId)
        {
            System.Diagnostics.Debug.WriteLine($"[GameDetailPage] Navigated to gameId: {gameId}");
            await ViewModel.LoadGameAsync(gameId);
            Bindings.Update(); // Force x:Bind to refresh after SelectedGame is set

            // Subscribe to property changes
            if (ViewModel.SelectedGame != null)
            {
                ViewModel.SelectedGame.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(GameViewModel.IsFavorite))
                    {
                        DispatcherQueue.TryEnqueue(UpdateFavoriteButton);
                    }
                };
            }

            UpdateFavoriteButton();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[GameDetailPage] Navigated without string parameter. Type: {e.Parameter?.GetType()}");
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
