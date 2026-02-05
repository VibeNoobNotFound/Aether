using Aether.WinUI.Models;
using Aether.WinUI.ViewModels;
using Aether.WinUI.Views;
using Aether.WinUI.Views.Search;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.ComponentModel;

namespace Aether.WinUI;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel => (Application.Current as App)!.Services.GetRequiredService<MainViewModel>();
    public SearchViewModel SearchViewModel => (Application.Current as App)!.Services.GetRequiredService<SearchViewModel>();
    public SettingsViewModel SettingsViewModel => (Application.Current as App)!.Services.GetRequiredService<SettingsViewModel>();

    private bool _isSearchActive;
    private Type? _lastContentPageType;

    public MainWindow()
    {
        InitializeComponent();
        Title = "Aether";
        SettingsViewModel.PropertyChanged += SettingsViewModel_PropertyChanged;
        UpdateNavigationStyle();

        ViewModel.NavigateToGameDetailRequested += (s, gameId) =>
        {
            ClearSearchState();
            ContentFrame.Navigate(typeof(GameDetailPage), gameId);
            NavView.SelectedItem = null; // Clear selection as we are in detail view
            // Optionally enable back button
            title.IsBackButtonVisible = true;
        };

        ViewModel.NavigateToLibraryRequested += (s, e) =>
        {
            ClearSearchState();
            ContentFrame.Navigate(typeof(LibraryPage));
            // Update nav selection to Library
            foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
            {
                if (item.Tag?.ToString() == "Library")
                {
                    NavView.SelectedItem = item;
                    break;
                }
            }
            title.IsBackButtonVisible = false;
        };

        title.BackButtonClick += (_, _) =>
        {
            if (ContentFrame.CanGoBack)
                ContentFrame.GoBack();
            
            title.IsBackButtonVisible = false; // Hide back button after going back once

        };


        title.PaneButtonClick += (_, _) =>
        {
            if (NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Left)
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
            else if (NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.LeftCompact)
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
            // No action for Top mode
        };
        ContentFrame.Navigated += (_, _) =>
        {
            // Hide back button when navigating to main pages
            if (ContentFrame.CurrentSourcePageType == typeof(LibraryPage) ||
                ContentFrame.CurrentSourcePageType == typeof(SettingsPage) ||
                ContentFrame.CurrentSourcePageType == typeof(SearchResultsPage))
            {
               
                Background.Visibility = Visibility.Collapsed;
            }
            else
            {
                Background.Visibility = Visibility.Visible;
            }

        };
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Navigate to Home by default
        NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().First();
        ContentFrame.Navigate(typeof(HomePage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        ClearSearchState();
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            ViewModel.CurrentScreen = AppScreen.Settings;
        }
        else if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            switch (tag)
            {
                case "Home":
                    ContentFrame.Navigate(typeof(HomePage));
                    ViewModel.CurrentScreen = AppScreen.Home;
                    break;
                case "Library":
                    ContentFrame.Navigate(typeof(LibraryPage));
                    ViewModel.CurrentScreen = AppScreen.Library;
                    break;
                case "Store":
                    // ContentFrame.Navigate(typeof(StorePage)); 
                    // Temporary placeholder
                    ContentFrame.Navigate(typeof(HomePage));
                    ViewModel.CurrentScreen = AppScreen.Store;
                    break;
            }
            title.IsBackButtonVisible = false;
        }
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.ProgrammaticChange)
        {
            return;
        }

        SearchViewModel.Query = sender.Text ?? string.Empty;
        UpdateSearchNavigation();
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        SearchViewModel.Query = args.QueryText ?? string.Empty;
        UpdateSearchNavigation();
    }

    private void UpdateSearchNavigation()
    {
        var hasQuery = !string.IsNullOrWhiteSpace(SearchViewModel.Query);

        if (hasQuery && !_isSearchActive)
        {
            _lastContentPageType = ContentFrame.CurrentSourcePageType;
            ContentFrame.Navigate(typeof(SearchResultsPage));
            _isSearchActive = true;
            title.IsBackButtonVisible = false;
        }
        else if (!hasQuery && _isSearchActive)
        {
            _isSearchActive = false;
            NavigateToCurrentScreen();
        }
    }

    private void ClearSearchState()
    {
        if (_isSearchActive || !string.IsNullOrWhiteSpace(SearchViewModel.Query))
        {
            SearchViewModel.ClearSearch();
            if (SearchBox != null)
            {
                SearchBox.Text = string.Empty;
            }
            _isSearchActive = false;
        }
    }

    private void NavigateToCurrentScreen()
    {
        switch (ViewModel.CurrentScreen)
        {
            case AppScreen.Home:
                ContentFrame.Navigate(typeof(HomePage));
                break;
            case AppScreen.Library:
                ContentFrame.Navigate(typeof(LibraryPage));
                break;
            case AppScreen.Settings:
                ContentFrame.Navigate(typeof(SettingsPage));
                break;
            case AppScreen.Store:
                ContentFrame.Navigate(typeof(HomePage));
                break;
            default:
                if (_lastContentPageType != null)
                {
                    ContentFrame.Navigate(_lastContentPageType);
                }
                else
                {
                    ContentFrame.Navigate(typeof(HomePage));
                }
                break;
        }
    }

    private void SettingsViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.NavigationStyleIndex))
        {
            UpdateNavigationStyle();
        }
    }

    private void UpdateNavigationStyle()
    {
        var useTop = SettingsViewModel.NavigationStyleIndex == 1;
        NavView.PaneDisplayMode = useTop ? NavigationViewPaneDisplayMode.Top : NavigationViewPaneDisplayMode.Left;
        NavView.IsPaneToggleButtonVisible = !useTop;
        title.IsPaneButtonVisible = !useTop;
    }
}
