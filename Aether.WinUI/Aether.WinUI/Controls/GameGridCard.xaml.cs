using Aether.WinUI.Models;
using Aether.WinUI.Services;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Aether.WinUI.Views.Library;

namespace Aether.WinUI.Controls;

public sealed partial class GameGridCard : UserControl
{
    public GameViewModel Game { get { return (GameViewModel)GetValue(GameProperty); } set { SetValue(GameProperty, value); } }
    public static readonly DependencyProperty GameProperty = DependencyProperty.Register("Game", typeof(GameViewModel), typeof(GameGridCard), new PropertyMetadata(null));

    public MainViewModel MainViewModel => (Application.Current as App)!.Services.GetRequiredService<MainViewModel>();

    public GameGridCard()
    {
        this.InitializeComponent();
    }

    private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string action && Game != null)
        {
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
                        System.Diagnostics.Debug.WriteLine($"Error showing properties: {ex}");
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

    private void Grid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        ScaleTransform.ScaleX = 1.05;
        ScaleTransform.ScaleY = 1.05;
    }

    private void Grid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        ScaleTransform.ScaleX = 1.0;
        ScaleTransform.ScaleY = 1.0;
    }
}
