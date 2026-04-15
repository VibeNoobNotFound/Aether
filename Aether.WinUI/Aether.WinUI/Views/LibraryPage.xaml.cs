using Aether.WinUI.Controls;
using Aether.WinUI.Models;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aether.WinUI.Views;

public sealed partial class LibraryPage : Page
{
    public MainViewModel ViewModel => Ioc.Default.GetRequiredService<MainViewModel>();
    private readonly ILogger<LibraryPage> _logger;

    public LibraryPage()
    {
        this.InitializeComponent();
        _logger = Ioc.Default.GetRequiredService<ILogger<LibraryPage>>();
        _logger.LogDebug("LibraryPage initialized");
    }

    private void AddGameMenu_Opening(object sender, object e)
    {
        _logger.LogInformation("AddGameMenu opening");
        var menu = sender as MenuFlyout;
        if (menu == null) return;
        // Keep the first 2 items (Scan + Separator)
        // Remove old dynamic items
        while (menu.Items.Count > 2)
        {
            menu.Items.RemoveAt(menu.Items.Count - 1);
        }

        foreach (var plugin in ViewModel.Plugins.Where(p => p.SupportsManualAddition))
        {
            _logger.LogDebug("Adding plugin manual addition menu: {Plugin}", plugin.Name);
            var item = new MenuFlyoutItem { Text = plugin.Name, Icon = new FontIcon { Glyph = "\uE710" } }; // Plus icon
            item.Click += async (s, args) =>
            {
                _logger.LogInformation("Manual add clicked for plugin: {Plugin}", plugin.Name);
                var dialog = new Library.LibraryAddMenu(plugin.Name);
                dialog.XamlRoot = this.XamlRoot;
                await dialog.ShowAsync();
            };
            menu.Items.Add(item);
        }
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
