using Aether.WinUI.Models;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

namespace Aether.WinUI.Views.Library;

public sealed partial class CollectionDetailDialog : ContentDialog
{
    public CollectionEditorViewModel ViewModel { get; }
    private readonly ILogger<CollectionDetailDialog> _logger;

    public CollectionDetailDialog(CollectionViewModel collection, MainViewModel mainVm)
    {
        this.InitializeComponent();
        _logger = Ioc.Default.GetRequiredService<ILogger<CollectionDetailDialog>>();
        _logger.LogDebug("CollectionDetailDialog initialized");
        var vmLogger = Ioc.Default.GetRequiredService<ILogger<CollectionEditorViewModel>>();
        ViewModel = new CollectionEditorViewModel(collection, mainVm, vmLogger);
        ViewModel.IsWorkDone = false;
        this.PrimaryButtonClick += CollectionDetailDialog_PrimaryButtonClick;
        this.Loaded += CollectionDetailDialog_Loaded;
    }

    private void CollectionDetailDialog_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("CollectionDetailDialog loaded");
        // Must manually sync selection because ListView SelectedItems binding is tricky
        if (ViewModel.IsCustomCollection && GamesList != null)
        {
            foreach (var game in ViewModel.AllGames)
            {
                if (ViewModel.SelectedGameIds.Contains(game.Id))
                {
                    GamesList.SelectedItems.Add(game);
                }
            }
        }
    }

    private async void CollectionDetailDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        _logger.LogInformation("CollectionDetailDialog primary button clicked");
        var deferral = args.GetDeferral();
        try
        {
            // Sync selection back to VM properties
            if (ViewModel.IsCustomCollection && GamesList != null)
            {
                ViewModel.UpdateSelectedGames(GamesList.SelectedItems.Cast<GameViewModel>().Select(g => g.Id));
            }

            await ViewModel.SaveAsync();
        }
        finally
        {
            deferral.Complete();
            ViewModel.IsWorkDone = false;
        }
    }

    private async void IconPicker_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("IconPicker clicked");
        var iconPicker = new IconPickerDialog();
        iconPicker.XamlRoot = this.XamlRoot;

        // Hide current dialog
        this.Hide();
        await iconPicker.ShowAsync();

        // Re-show current dialog
        await this.ShowAsync();

        if (!string.IsNullOrEmpty(iconPicker.SelectedIconGlyph))
        {
            ViewModel.IconGlyph = iconPicker.SelectedIconGlyph;
        }
    }
}
