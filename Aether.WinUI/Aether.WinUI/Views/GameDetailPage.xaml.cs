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
            FavoriteText.Text = "Unfavorite"; // Could bind this too
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

        ResetLogoState();

        if (e.Parameter is string gameId)
        {
            System.Diagnostics.Debug.WriteLine($"[GameDetailPage] Navigated to gameId: {gameId}");
            await ViewModel.LoadGameAsync(gameId);
            Bindings.Update();

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
    }

    private void MainScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (StickyHeader == null) return;

        // Simple threshold for sticky header acrylic
        if (MainScrollViewer.VerticalOffset > 10)
        {
            StickyHeader.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DarkAcrylicBackgroundBrush"];
        }
        else
        {
            StickyHeader.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }
    }

    private void LogoImage_ImageOpened(object sender, RoutedEventArgs e)
    {
        if (LogoImage != null) LogoImage.Visibility = Visibility.Visible;
        if (TitleText != null) TitleText.Visibility = Visibility.Collapsed;
    }

    private void ResetLogoState()
    {
        if (LogoImage != null) LogoImage.Visibility = Visibility.Collapsed;
        if (TitleText != null) TitleText.Visibility = Visibility.Visible;
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedGame == null) return;

        var dialog = new Aether.WinUI.Views.Library.MetadataEditorDialog(ViewModel.SelectedGame);
        dialog.XamlRoot = this.XamlRoot;
        await dialog.ShowAsync();
    }
}
