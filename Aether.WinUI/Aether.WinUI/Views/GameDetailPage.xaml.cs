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
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media.Core;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.Logging;

namespace Aether.WinUI.Views;

public sealed partial class GameDetailPage : Page
{
    private GameDetailViewModel? _viewModel;
    public GameDetailViewModel ViewModel => _viewModel ??= Ioc.Default.GetRequiredService<GameDetailViewModel>();
    private ImageCacheService ImageCache => Ioc.Default.GetRequiredService<ImageCacheService>();
    private readonly ILogger<GameDetailPage> _logger;

    public GameDetailPage()
    {
        this.InitializeComponent();
        this.Loaded += GameDetailPage_Loaded;
        _logger = Ioc.Default.GetRequiredService<ILogger<GameDetailPage>>();
        _logger.LogDebug("GameDetailPage initialized");
    }

    private void GameDetailPage_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.LogDebug("GameDetailPage loaded");
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
            _logger.LogInformation("Navigated to GameDetailPage: {GameId}", gameId);
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
        _logger.LogTrace("MainScrollViewer_ViewChanged");
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Properties button clicked");
        if (ViewModel.SelectedGame == null) return;

        var dialog = new Aether.WinUI.Views.Library.MetadataEditorDialog(ViewModel.SelectedGame);
        dialog.XamlRoot = this.XamlRoot;
        await dialog.ShowAsync();
    }

    private async void StopGame_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Stop game clicked");
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
                // ignore invalid URLs
            }
        }
    }

    private async void MediaItem_Click(object sender, ItemClickEventArgs e)
    {
        _logger.LogInformation("Media item clicked");
        if (e.ClickedItem is not MediaItemViewModel media) return;

        var items = ViewModel.MediaItems?.ToList() ?? new List<MediaItemViewModel>();
        if (items.Count == 0) return;

        var index = items.IndexOf(media);
        if (index < 0) index = 0;

        var contentHost = new ContentControl();
        var prevButton = new Button { Content = "Previous" };
        var nextButton = new Button { Content = "Next" };

        var navPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 0)
        };
        navPanel.Children.Add(prevButton);
        navPanel.Children.Add(nextButton);

        var root = new Grid { RowDefinitions = { new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, new RowDefinition { Height = GridLength.Auto } } };
        root.Background = new SolidColorBrush(Microsoft.UI.Colors.Black);
        root.Children.Add(contentHost);
        Grid.SetRow(navPanel, 1);
        root.Children.Add(navPanel);

        var dialog = new ContentDialog
        {
            Content = root,
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot,
            MinWidth = 720,
            MinHeight = 480,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(12)
        };

        if (XamlRoot != null)
        {
            dialog.MaxWidth = Math.Max(720, XamlRoot.Size.Width - 40);
            dialog.MaxHeight = Math.Max(480, XamlRoot.Size.Height - 40);
        }

        async Task UpdateMediaAsync()
        {
            var current = items[index];
            contentHost.Content = await CreateMediaContentAsync(current);
            dialog.Title = $"{(current.IsVideo ? "Video" : "Screenshot")} {index + 1} of {items.Count}";
            prevButton.IsEnabled = index > 0;
            nextButton.IsEnabled = index < items.Count - 1;
        }

        prevButton.Click += async (_, _) =>
        {
            if (index <= 0) return;
            index--;
            await UpdateMediaAsync();
        };

        nextButton.Click += async (_, _) =>
        {
            if (index >= items.Count - 1) return;
            index++;
            await UpdateMediaAsync();
        };

        await UpdateMediaAsync();
        await dialog.ShowAsync();
    }

    private async Task<FrameworkElement> CreateMediaContentAsync(MediaItemViewModel media)
    {
        _logger.LogDebug("CreateMediaContentAsync: {Url}", media.Url);
        if (media.IsVideo)
        {
            return new MediaPlayerElement
            {
                Source = MediaSource.CreateFromUri(new Uri(media.Url)),
                AreTransportControlsEnabled = true,
                AutoPlay = true,
                Stretch = Stretch.Uniform
            };
        }

        var image = new Image { Stretch = Stretch.Uniform };
        image.Source = await ImageCache.GetImageAsync(media.Url);

        return new ScrollViewer
        {
            Content = image,
            ZoomMode = ZoomMode.Enabled,
            MinZoomFactor = 1,
            MaxZoomFactor = 4,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }
}
