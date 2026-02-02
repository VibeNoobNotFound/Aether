using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Aether.Protos;
using System.Collections.Generic;

namespace Aether.WinUI.Views.Home;

public sealed partial class CarouselEditorDialog : ContentDialog
{
    public MainViewModel ViewModel => (Application.Current as App)!.Services.GetRequiredService<MainViewModel>();

    public CarouselEditorDialog()
    {
        this.InitializeComponent();
        this.Loaded += CarouselEditorDialog_Loaded;
        this.PrimaryButtonClick += CarouselEditorDialog_PrimaryButtonClick;
    }

    private async void CarouselEditorDialog_Loaded(object sender, RoutedEventArgs e)
    {
        var config = await ViewModel.LoadCarouselConfigAsync();
        if (config != null)
        {
            MaxGamesBox.Value = config.MaxGames > 0 ? config.MaxGames : 5;
            
            if (config.HasCollectionId && config.CollectionId > 0) 
            {
                SourceComboBox.SelectedIndex = 1; // Collection
                // Wait for collections to be loaded if empty? Assuming populated
                var collection = ViewModel.Collections.FirstOrDefault(c => c.Id == config.CollectionId);
                if (collection != null)
                {
                    CollectionComboBox.SelectedItem = collection;
                }
            }
            else if (config.GameIds.Count > 0)
            {
                SourceComboBox.SelectedIndex = 2; // Manual
                // Select items in ListView
                foreach(var gameId in config.GameIds)
                {
                    var game = ViewModel.Games.FirstOrDefault(g => g.Id == gameId);
                    if (game != null)
                    {
                        if (GamesListView.SelectionMode == ListViewSelectionMode.Multiple)
                        {
                            GamesListView.SelectedItems.Add(game);
                        }
                    }
                }
            }
            else
            {
                SourceComboBox.SelectedIndex = 0; // Auto
            }
        }
        else
        {
            SourceComboBox.SelectedIndex = 0;
        }
        
        UpdateVisibility();
    }

    private async void CarouselEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();

        try
        {
            var config = new CarouselConfig
            {
                MaxGames = (int)MaxGamesBox.Value
            };

            int sourceIndex = SourceComboBox.SelectedIndex;
            if (sourceIndex == 1) // Collection
            {
                 if (CollectionComboBox.SelectedItem is Models.CollectionViewModel col)
                 {
                     config.CollectionId = col.Id;
                 }
            }
            else if (sourceIndex == 2) // Manual
            {
                config.GameIds.AddRange(GamesListView.SelectedItems.Cast<Models.GameViewModel>().Select(g => g.Id));
            }
            
            await ViewModel.SaveCarouselConfigAsync(config);
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void SourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (CollectionPanel == null || ManualPanel == null) return;

        var tag = (SourceComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        
        CollectionPanel.Visibility = Visibility.Collapsed;
        ManualPanel.Visibility = Visibility.Collapsed;

        if (tag == "1")
        {
            CollectionPanel.Visibility = Visibility.Visible;
        }
        else if (tag == "2")
        {
            ManualPanel.Visibility = Visibility.Visible;
        }
    }
}
