using Aether.WinUI.Models;
using Aether.WinUI.Services;
using Aether.WinUI.ViewModels;
using Aether.WinUI.Views.Library;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Numerics;
using System.Xml.Linq;

namespace Aether.WinUI.Controls;

public sealed partial class GameGridCard : Button
{
    public GameViewModel Game { get { return (GameViewModel)GetValue(GameProperty); } set { SetValue(GameProperty, value); } }
    public static readonly DependencyProperty GameProperty = DependencyProperty.Register("Game", typeof(GameViewModel), typeof(GameGridCard), new PropertyMetadata(null, OnGameChanged));

    public MainViewModel MainViewModel => Ioc.Default.GetRequiredService<MainViewModel>();
    private readonly ILogger<GameGridCard> _logger;

    public GameGridCard()
    {
        this.InitializeComponent();
        this.Loaded += GameGridCard_Loaded;
        _logger = Ioc.Default.GetRequiredService<ILogger<GameGridCard>>();
        _logger.LogDebug("GameGridCard initialized");
    }

    public Image CoverImageElement => CoverImage;

    private static void OnGameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GameGridCard card && e.NewValue is GameViewModel game)
        {
            card._logger.LogDebug("GameGridCard game changed: {GameId}", game.Id);
            card.UpdateFavoriteButton();
            game.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(GameViewModel.IsFavorite))
                {
                    card._logger.LogDebug("GameGridCard favorite changed: {GameId}", game.Id);
                    card.UpdateFavoriteButton();
                }
            };
        }
    }

    private void GameGridCard_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.LogDebug("GameGridCard loaded");
        UpdateFavoriteButton();
    }

    private void UpdateFavoriteButton()
    {
        if (Game == null || FavoriteMenuItem == null) return;
        _logger.LogDebug("GameGridCard updating favorite button: {GameId} IsFavorite={IsFavorite}", Game.Id, Game.IsFavorite);

        if (Game.IsFavorite)
        {
            FavoriteMenuItem.Text = "Remove from Favorites";
            FavoriteMenuItem.Icon = new FontIcon { Glyph = "\uE735" }; // Filled star
        }
        else
        {
            FavoriteMenuItem.Text = "Add to Favorites";
            FavoriteMenuItem.Icon = new FontIcon { Glyph = "\uE734" }; // Outline star
        }
    }

    private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string action && Game != null)
        {
            _logger.LogInformation("GameGridCard menu action: {Action} for {GameId}", action, Game.Id);
            switch (action)
            {
                case "Launch":
                    _ = MainViewModel.LaunchGameCommand.ExecuteAsync(Game.Id);
                    break;
                case "Stop":
                    _ = MainViewModel.StopGameCommand.ExecuteAsync(Game.Id);
                    break;
                case "Properties":
                    try
                    {
                        var dialog = new MetadataEditorDialog(Game);
                        dialog.XamlRoot = this.XamlRoot;
                        await dialog.ShowAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error showing properties for {GameId}", Game.Id);
                    }
                    break;
                case "Favorite":
                    _ = MainViewModel.ToggleFavoriteCommand.ExecuteAsync(Game.Id);
                    break;
                case "ShowInExplorer":
                    _ = MainViewModel.OpenGameLocationCommand.ExecuteAsync(Game.Id);
                    break;
                case "Remove":
                    var deleteDialog = new ContentDialog
                    {
                        Title = "Remove Game?",
                        Content = $"Are you sure you want to remove {Game.Title} from your library? This will not uninstall the game files.",
                        PrimaryButtonText = "Remove",
                        CloseButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.XamlRoot
                    };
                    if (await deleteDialog.ShowAsync() == ContentDialogResult.Primary)
                    {
                        _ = MainViewModel.RemoveGameCommand.ExecuteAsync(Game.Id);
                    }
                    break;
                    // TODO: Implement other actions
            }
        }
    }
    Compositor _compositor = App.Current.MainWindow.Compositor;
    SpringVector3NaturalMotionAnimation _springAnimation;

    private void CreateOrUpdateSpringAnimation(float finalValue)
    {
        _logger.LogDebug("GameGridCard spring animation updated: {FinalValue}", finalValue);
        if (_springAnimation == null)
        {
            _springAnimation = _compositor.CreateSpringVector3Animation();
            _springAnimation.Target = "Scale";
        }

        _springAnimation.FinalValue = new Vector3(finalValue);
    }

    private void Grid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _logger.LogDebug("GameGridCard pointer entered");
        CreateOrUpdateSpringAnimation(1.05f);

        (sender as UIElement).StartAnimation(_springAnimation);

    }

    private void Grid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _logger.LogDebug("GameGridCard pointer exited");
        CreateOrUpdateSpringAnimation(1.0f);

        (sender as UIElement).StartAnimation(_springAnimation);

    }
}
