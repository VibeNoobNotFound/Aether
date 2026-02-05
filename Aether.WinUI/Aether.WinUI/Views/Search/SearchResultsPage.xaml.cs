using Aether.WinUI.Controls;
using Aether.WinUI.Models;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.ComponentModel;

namespace Aether.WinUI.Views.Search;

public sealed partial class SearchResultsPage : Page
{
    public SearchViewModel ViewModel => Ioc.Default.GetRequiredService<SearchViewModel>();
    public MainViewModel MainViewModel => Ioc.Default.GetRequiredService<MainViewModel>();
    private readonly ILogger<SearchResultsPage> _logger;

    public SearchResultsPage()
    {
        InitializeComponent();
        Loaded += SearchResultsPage_Loaded;
        _logger = Ioc.Default.GetRequiredService<ILogger<SearchResultsPage>>();
        _logger.LogDebug("SearchResultsPage initialized");
    }

    private void SearchResultsPage_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("SearchResultsPage loaded");
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        UpdatePlatformSelection();
    }

    private void PlatformFilter_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Platform filter clicked");
        if (sender is ToggleButton toggle && toggle.Tag is string platform)
        {
            ViewModel.TogglePlatformFilter(platform);
            UpdatePlatformSelection();
        }
    }

    private void PlatformFilter_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.LogTrace("Platform filter loaded");
        if (sender is ToggleButton toggle && toggle.Tag is string platform)
        {
            toggle.IsChecked = string.Equals(ViewModel.FilterPlatform, platform, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _logger.LogTrace("ViewModel property changed: {PropertyName}", e.PropertyName);
        if (e.PropertyName == nameof(ViewModel.FilterPlatform))
        {
            UpdatePlatformSelection();
        }
    }

    private void UpdatePlatformSelection()
    {
        _logger.LogTrace("UpdatePlatformSelection invoked");
        if (ImporterFilters?.ItemsPanelRoot is not Panel panel) return;

        foreach (var child in panel.Children)
        {
            var toggle = FindVisualChild<ToggleButton>(child);
            if (toggle?.Tag is string platform)
            {
                toggle.IsChecked = string.Equals(ViewModel.FilterPlatform, platform, System.StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                return match;
            }

            var result = FindVisualChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    private void GamegCard_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Search result game clicked");
        if (sender is GameGridCard card)
        {
            if (card.CoverImageElement != null)
            {
                Microsoft.UI.Xaml.Media.Animation.ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("CoverAnimation", card.CoverImageElement);
            }
            MainViewModel.GoToGameDetailCommand.Execute(card.Game.Id);
        }
    }
}
