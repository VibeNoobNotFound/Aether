using Aether.Protos;
using Aether.WinUI.Models;
using Aether.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aether.WinUI.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly GrpcClientService _grpc;
    private readonly MainViewModel _mainViewModel;
    private readonly DispatcherQueue _dispatcher;

    private CancellationTokenSource? _searchCts;
    private NotifyCollectionChangedEventHandler? _pluginsChangedHandler;

    [ObservableProperty] private string query = "";
    [ObservableProperty] private ObservableCollection<GameViewModel> results = new();
    [ObservableProperty] private int totalMatches;
    [ObservableProperty] private bool isSearching;
    [ObservableProperty] private string? errorMessage;

    // Filters
    [ObservableProperty] private string? filterPlatform;
    [ObservableProperty] private string? filterGenre;

    // Sorting
    [ObservableProperty] private LibrarySearchRequest.Types.SortOption sortBy = LibrarySearchRequest.Types.SortOption.Relevance;
    [ObservableProperty] private bool sortAscending;

    [ObservableProperty] private bool showEmptyState;
    [ObservableProperty] private bool showProgress;

    public ObservableCollection<PluginViewModel> AvailableImporters { get; } = new();

    public SearchViewModel(GrpcClientService grpc, MainViewModel mainViewModel)
    {
        _grpc = grpc;
        _mainViewModel = mainViewModel;
        _dispatcher = App.Current?.MainWindow?.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();

        HookPluginCollection();
        RefreshAvailableImporters();
    }

    public int SortByIndex
    {
        get => SortBy switch
        {
            LibrarySearchRequest.Types.SortOption.Name => 1,
            LibrarySearchRequest.Types.SortOption.ReleaseDate => 2,
            LibrarySearchRequest.Types.SortOption.Playtime => 3,
            _ => 0
        };
        set
        {
            SortBy = value switch
            {
                1 => LibrarySearchRequest.Types.SortOption.Name,
                2 => LibrarySearchRequest.Types.SortOption.ReleaseDate,
                3 => LibrarySearchRequest.Types.SortOption.Playtime,
                _ => LibrarySearchRequest.Types.SortOption.Relevance
            };
            OnPropertyChanged(nameof(SortByIndex));
        }
    }

    partial void OnQueryChanged(string value)
    {
        DebounceSearch();
        UpdateEmptyState();
    }

    partial void OnFilterPlatformChanged(string? value)
    {
        DebounceSearch();
    }

    partial void OnFilterGenreChanged(string? value)
    {
        DebounceSearch();
    }

    partial void OnSortByChanged(LibrarySearchRequest.Types.SortOption value)
    {
        DebounceSearch();
        OnPropertyChanged(nameof(SortByIndex));
    }

    partial void OnSortAscendingChanged(bool value)
    {
        DebounceSearch();
    }

    public void ClearFilters()
    {
        FilterPlatform = null;
        FilterGenre = null;
    }

    public void ClearSearch()
    {
        _searchCts?.Cancel();
        Query = string.Empty;
        FilterPlatform = null;
        FilterGenre = null;
        Results.Clear();
        TotalMatches = 0;
        ErrorMessage = null;
        IsSearching = false;
        ShowProgress = false;
        ShowEmptyState = false;
    }

    public void TogglePlatformFilter(string platform)
    {
        if (string.Equals(FilterPlatform, platform, StringComparison.OrdinalIgnoreCase))
        {
            FilterPlatform = null;
        }
        else
        {
            FilterPlatform = platform;
        }
    }

    private void DebounceSearch()
    {
        _searchCts?.Cancel();

        if (string.IsNullOrWhiteSpace(Query) && FilterPlatform == null && FilterGenre == null)
        {
            Results.Clear();
            TotalMatches = 0;
            UpdateEmptyState();
            return;
        }

        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                if (token.IsCancellationRequested) return;

                await PerformSearchAsync(token);
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
        }, token);
    }

    private async Task PerformSearchAsync(CancellationToken token)
    {
        try
        {
            await EnqueueAsync(() =>
            {
                IsSearching = true;
                ShowProgress = Results.Count == 0;
                ErrorMessage = null;
            });

            var request = new LibrarySearchRequest
            {
                Query = Query ?? string.Empty,
                SortBy = SortBy,
                SortAscending = SortAscending,
                Limit = 50
            };

            if (!string.IsNullOrWhiteSpace(FilterPlatform))
            {
                request.FilterPlatforms.Add(FilterPlatform);
            }

            if (!string.IsNullOrWhiteSpace(FilterGenre))
            {
                request.FilterGenres.Add(FilterGenre);
            }

            var response = await _grpc.Client.SearchLibraryAsync(request, cancellationToken: token);

            if (token.IsCancellationRequested) return;

            var mapped = response.Games.Select(GameViewModel.FromProto).ToList();

            await EnqueueAsync(() =>
            {
                Results = new ObservableCollection<GameViewModel>(mapped);
                TotalMatches = response.TotalMatches;
                IsSearching = false;
                ShowProgress = false;
                UpdateEmptyState();
            });
        }
        catch (Exception ex)
        {
            if (token.IsCancellationRequested) return;

            await EnqueueAsync(() =>
            {
                IsSearching = false;
                ShowProgress = false;
                ErrorMessage = ex.Message;
                UpdateEmptyState();
            });
        }
    }

    private void HookPluginCollection()
    {
        _mainViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.Plugins))
            {
                RefreshAvailableImporters();
                AttachPluginsCollection();
            }
        };

        AttachPluginsCollection();
    }

    private void AttachPluginsCollection()
    {
        if (_pluginsChangedHandler != null)
        {
            _mainViewModel.Plugins.CollectionChanged -= _pluginsChangedHandler;
        }

        _pluginsChangedHandler = (_, _) => RefreshAvailableImporters();
        _mainViewModel.Plugins.CollectionChanged += _pluginsChangedHandler;
    }

    private void RefreshAvailableImporters()
    {
        var importers = _mainViewModel.Plugins
            .Where(p => p.Capabilities.Contains("Importer"))
            .OrderBy(p => p.Name)
            .ToList();

        _ = EnqueueAsync(() =>
        {
            AvailableImporters.Clear();
            foreach (var plugin in importers)
            {
                AvailableImporters.Add(plugin);
            }
        });
    }

    private void UpdateEmptyState()
    {
        ShowEmptyState = !IsSearching && Results.Count == 0 && !string.IsNullOrWhiteSpace(Query);
    }

    private Task EnqueueAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        _dispatcher.TryEnqueue(() =>
        {
            action();
            tcs.SetResult(true);
        });
        return tcs.Task;
    }
}
