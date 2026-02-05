using Aether.WinUI.Models;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace Aether.WinUI.Views.Library;

public sealed partial class CollectionManagerDialog : ContentDialog
{
    public MainViewModel ViewModel => Ioc.Default.GetRequiredService<MainViewModel>();
    private readonly ILogger<CollectionManagerDialog> _logger;

    public CollectionManagerDialog()
    {
        this.InitializeComponent();
        _logger = Ioc.Default.GetRequiredService<ILogger<CollectionManagerDialog>>();
        _logger.LogDebug("CollectionManagerDialog initialized");
    }

    private async void NewCollection_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("NewCollection clicked");
        var newCollection = new CollectionViewModel
        {
            Name = "New Collection",
            Icon = "\uE8B7", // Folder
            Type = Protos.CollectionType.CollectionCustom
        };

        var dialog = new CollectionDetailDialog(newCollection, ViewModel);
        dialog.XamlRoot = this.XamlRoot;

        // Hide parent dialog before opening child
        this.Hide();
        _ =  dialog.ShowAsync();
        await dialog.ViewModel.WaitForWorkToFinishAsync();

        // Re-show parent dialog
        _ = this.ShowAsync();

        if (dialog.ViewModel.Success)
        {
            // If saved, call backend Create
            await ViewModel.CreateCollectionAsync(newCollection.Name, newCollection.Icon, dialog.ViewModel.SelectedGameIds.ToList());
        }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Edit collection clicked");
        if ((sender as Button)?.Tag is CollectionViewModel collection)
        {
            var dialog = new CollectionDetailDialog(collection, ViewModel);
            dialog.XamlRoot = this.XamlRoot;

            // Hide parent dialog before opening child
            this.Hide();
            _ = dialog.ShowAsync();
            await dialog.ViewModel.WaitForWorkToFinishAsync();

            // Re-show parent dialog
            _ = this.ShowAsync();
            // Edits are saved inside detail dialog via ViewModel.SaveAsync
        }
    }

    private async void Visibility_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Toggle visibility clicked");
        if ((sender as Button)?.Tag is CollectionViewModel collection)
        {
            await ViewModel.ToggleCollectionVisibilityAsync(collection);
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Delete collection clicked");
        if ((sender as Button)?.Tag is CollectionViewModel collection)
        {
            // Confirmation?
            // For now direct delete similar to macOS list swipe
            await ViewModel.DeleteCollectionAsync(collection.Id);
        }
    }
}
