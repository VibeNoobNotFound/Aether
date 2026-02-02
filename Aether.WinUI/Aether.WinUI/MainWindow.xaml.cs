using Aether.WinUI.Models;
using Aether.WinUI.ViewModels;
using Aether.WinUI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

namespace Aether.WinUI;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel => (Application.Current as App)!.Services.GetRequiredService<MainViewModel>();

    public MainWindow()
    {
        InitializeComponent();
        Title = "Aether";

        ViewModel.NavigateToGameDetailRequested += (s, gameId) =>
        {
            ContentFrame.Navigate(typeof(GameDetailPage), gameId);
            NavView.SelectedItem = null; // Clear selection as we are in detail view
            // Optionally enable back button
            title.IsBackButtonVisible = true;
        };

        ViewModel.NavigateToLibraryRequested += (s, e) =>
        {
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

    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Navigate to Home by default
        NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().First();
        ContentFrame.Navigate(typeof(HomePage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
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
}
