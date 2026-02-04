using Aether.WinUI.Models;
using Aether.WinUI.Services;
using Aether.WinUI.AttachedProperties;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;
using System.Linq;
using Windows.Media.Core;
using Microsoft.UI.Xaml.Media;

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

        if (e.Parameter is string gameId)
        {
            System.Diagnostics.Debug.WriteLine($"[GameDetailPage] Navigated to gameId: {gameId}");

            // Connected Animation
            var anim = Microsoft.UI.Xaml.Media.Animation.ConnectedAnimationService.GetForCurrentView().GetAnimation("CoverAnimation");
            if (anim != null)
            {
                anim.TryStart(CoverImage);
            }

            await ViewModel.LoadGameAsync(gameId);
            Bindings.Update();

            if (ViewModel.SelectedGame != null)
            {
                var bg = ViewModel.SelectedGame.DisplayBackgroundImageUrl;
                if (!string.IsNullOrWhiteSpace(bg))
                {
                    ViewModel.MainViewModel.WindowBackgroundImageUrl = bg;
                    ViewModel.MainViewModel.WindowBackgroundOpacity = 0.45;
                }

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
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedGame == null) return;

        var dialog = new Aether.WinUI.Views.Library.MetadataEditorDialog(ViewModel.SelectedGame);
        dialog.XamlRoot = this.XamlRoot;
        await dialog.ShowAsync();
    }

    private async void StopGame_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedGame == null) return;

        var processes = await ViewModel.MainViewModel.GetActiveProcessesAsync(ViewModel.SelectedGame.Id);
        if (processes == null || processes.Processes.Count == 0)
        {
            await ViewModel.MainViewModel.StopGameCommand.ExecuteAsync(ViewModel.SelectedGame.Id);
            return;
        }

        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(new TextBlock
        {
            Text = "This will forcefully terminate the game. Unsaved progress may be lost.",
            TextWrapping = TextWrapping.Wrap
        });

        var list = new ListView
        {
            Height = 200,
            ItemsSource = processes.Processes.Select(p => $"{p.ProcessName} (PID: {p.ProcessId})").ToList()
        };

        content.Children.Add(list);

        var dialog = new ContentDialog
        {
            Title = $"Stop {ViewModel.SelectedGame.Title}?",
            Content = content,
            PrimaryButtonText = "Force Stop",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.MainViewModel.StopGameCommand.ExecuteAsync(ViewModel.SelectedGame.Id);
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
                // ignore invalid URLs
            }
        }
    }

    private async void MediaItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not MediaItemViewModel media) return;

        FrameworkElement content;
        if (media.IsVideo)
        {
            var player = new MediaPlayerElement
            {
                Source = MediaSource.CreateFromUri(new Uri(media.Url)),
                AreTransportControlsEnabled = true,
                AutoPlay = true
            };
            content = player;
        }
        else
        {
            var image = new Image
            {
                Stretch = Stretch.Uniform
            };
            image.Source = await ImageCache.GetImageAsync(media.Url);
            content = image;
        }

        var dialog = new ContentDialog
        {
            Title = media.IsVideo ? "Video" : "Screenshot",
            Content = content,
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }
}
