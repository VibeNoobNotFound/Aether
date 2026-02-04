using Aether.WinUI.Models;
using Aether.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace Aether.WinUI.Views.Library;

public sealed partial class CollectionManagerDialog : ContentDialog
{
    public MainViewModel ViewModel => (Application.Current as App)!.Services.GetRequiredService<MainViewModel>();

    public CollectionManagerDialog()
    {
        this.InitializeComponent();
    }

    private async void NewCollection_Click(object sender, RoutedEventArgs e)
    {
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
        if ((sender as Button)?.Tag is CollectionViewModel collection)
        {
            await ViewModel.ToggleCollectionVisibilityAsync(collection);
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is CollectionViewModel collection)
        {
            // Confirmation?
            // For now direct delete similar to macOS list swipe
            await ViewModel.DeleteCollectionAsync(collection.Id);
        }
    }
}
