using Aether.Protos;
using Aether.WinUI.Models;
using Aether.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Aether.WinUI.ViewModels;

public partial class CollectionEditorViewModel : ObservableObject
{
    private readonly CollectionViewModel _originalCollection;
    private readonly MainViewModel _mainViewModel; // For accessing games list
    private readonly ILogger<CollectionEditorViewModel> _logger;

    [ObservableProperty] private string name;
    [ObservableProperty] private bool isWorkDone;
    [ObservableProperty] private bool success;
    [ObservableProperty] private string iconGlyph;
    [ObservableProperty] private bool canEditName;
    [ObservableProperty] private bool isCustomCollection; // vs System

    public ObservableCollection<GameViewModel> AllGames => _mainViewModel.Games;

    // Track selected IDs manually since ListView binding is read-only for SelectedItems
    public HashSet<string> SelectedGameIds { get; private set; } = new();

    public CollectionEditorViewModel(CollectionViewModel collection, MainViewModel mainVm, ILogger<CollectionEditorViewModel> logger)
    {
        _originalCollection = collection;
        _mainViewModel = mainVm;
        _logger = logger;
        _logger.LogDebug("CollectionEditorViewModel initialized for collection {CollectionId}", collection.Id);

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
    public Task WaitForWorkToFinishAsync()
    {
        _logger.LogDebug("WaitForWorkToFinishAsync invoked");
        if (IsWorkDone) return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        // Local function to handle the event/property change
        void Handler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsWorkDone) && IsWorkDone)
            {
                this.PropertyChanged -= Handler; // Clean up
                tcs.TrySetResult(true);
            }
        }

        this.PropertyChanged += Handler;
        return tcs.Task;
    }
    private static string MapIconToGlyph(string iconName)
    {
        Ioc.Default.GetService<ILogger<CollectionEditorViewModel>>()?
            .LogTrace("MapIconToGlyph iconName={IconName}", iconName);
        var mapper = Ioc.Default.GetService<IconMapService>();
        return mapper?.ToGlyph(iconName, iconName) ?? iconName;
    }

    public void UpdateSelectedGames(IEnumerable<string> newSelection)
    {
        _logger.LogDebug("UpdateSelectedGames invoked");
        SelectedGameIds.Clear();
        foreach (var id in newSelection) SelectedGameIds.Add(id);
    }

    public async Task SaveAsync()
    {
        _logger.LogInformation("SaveAsync invoked for collection {CollectionId}", _originalCollection.Id);
        var client = Ioc.Default.GetRequiredService<Services.GrpcClientService>().Client;

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
                    _logger.LogDebug("Added game to collection {CollectionId}: {GameId}", _originalCollection.Id, id);
                }
                foreach (var id in toRemove)
                {
                    // Fix: Use CollectionGameAction
                    await client.RemoveGameFromCollectionAsync(new CollectionGameAction { CollectionId = _originalCollection.Id, GameId = id });
                    _originalCollection.GameIds.Remove(id);
                    _logger.LogDebug("Removed game from collection {CollectionId}: {GameId}", _originalCollection.Id, id);
                }
            }
            Success = true;
            _logger.LogInformation("Collection saved {CollectionId}", _originalCollection.Id);
        }
        catch (Exception ex)
        {
            // Handle error
            _logger.LogError(ex, "Failed to save collection {CollectionId}", _originalCollection.Id);
        }
    }
}
