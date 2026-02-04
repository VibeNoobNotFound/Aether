using Aether.Protos;
using Aether.WinUI.Services;
using Aether.WinUI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Aether.WinUI.ViewModels;

public partial class CollectionEditorViewModel : ObservableObject
{
    private readonly CollectionViewModel _originalCollection;
    private readonly MainViewModel _mainViewModel; // For accessing games list

    [ObservableProperty] private string name;
    [ObservableProperty] private string iconGlyph;
    [ObservableProperty] private bool canEditName;
    [ObservableProperty] private bool isCustomCollection; // vs System

    public ObservableCollection<GameViewModel> AllGames => _mainViewModel.Games;

    // Track selected IDs manually since ListView binding is read-only for SelectedItems
    public HashSet<string> SelectedGameIds { get; private set; } = new();

    public CollectionEditorViewModel(CollectionViewModel collection, MainViewModel mainVm)
    {
        _originalCollection = collection;
        _mainViewModel = mainVm;

        Name = collection.Name;
        // In WinUI implementation we use FontIcon glyphs.
        // Assuming the model stores either SF Symbol name (legacy) or Glyph.
        // For now, let's assume we store the Glyph. If it's a legacy name, we might default.
        // For new items, we store the Glyph directly.
        IconGlyph = MapIconToGlyph(collection.Icon);

        CanEditName = !collection.IsSystem;
        IsCustomCollection = collection.Type == CollectionType.CollectionCustom; // Fixed Enum

        if (IsCustomCollection)
        {
            foreach (var id in collection.GameIds)
            {
                SelectedGameIds.Add(id);
            }
        }
    }

    private static string MapIconToGlyph(string iconName)
    {
        var app = Application.Current as App;
        var mapper = app?.Services.GetService<IconMapService>();
        return mapper?.ToGlyph(iconName, iconName) ?? iconName;
    }

    public void UpdateSelectedGames(IEnumerable<string> newSelection)
    {
        SelectedGameIds.Clear();
        foreach (var id in newSelection) SelectedGameIds.Add(id);
    }

    public async Task SaveAsync()
    {
        var client = (Application.Current as App)!.Services.GetRequiredService<Services.GrpcClientService>().Client;

        // Optimistic update
        _originalCollection.Name = Name;
        _originalCollection.Icon = IconGlyph;

        try
        {
            // Fix: UpdateCollectionRequest
            await client.UpdateCollectionAsync(new UpdateCollectionRequest
            {
                Id = _originalCollection.Id,
                Name = Name,
                IconName = IconGlyph
            });

            if (IsCustomCollection)
            {
                var currentIds = new HashSet<string>(_originalCollection.GameIds);
                var newIds = SelectedGameIds;

                var toAdd = newIds.Except(currentIds).ToList();
                var toRemove = currentIds.Except(newIds).ToList();

                foreach (var id in toAdd)
                {
                    // Fix: Use CollectionGameAction
                    await client.AddGameToCollectionAsync(new CollectionGameAction { CollectionId = _originalCollection.Id, GameId = id });
                    _originalCollection.GameIds.Add(id);
                }
                foreach (var id in toRemove)
                {
                    // Fix: Use CollectionGameAction
                    await client.RemoveGameFromCollectionAsync(new CollectionGameAction { CollectionId = _originalCollection.Id, GameId = id });
                    _originalCollection.GameIds.Remove(id);
                }
            }
        }
        catch
        {
            // Handle error
        }
    }
}
