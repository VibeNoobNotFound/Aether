using Aether.Protos;
using Aether.WinUI.Models;
using Aether.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<SearchViewModel> _logger;

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

    public SearchViewModel(GrpcClientService grpc, MainViewModel mainViewModel, ILogger<SearchViewModel> logger)
    {
        _grpc = grpc;
        _mainViewModel = mainViewModel;
        _logger = logger;
        _dispatcher = App.Current?.MainWindow?.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();

        _logger.LogDebug("SearchViewModel initialized");
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
        _logger.LogTrace("Query changed: {Query}", value);
        DebounceSearch();
        UpdateEmptyState();
    }

    partial void OnFilterPlatformChanged(string? value)
    {
        _logger.LogTrace("FilterPlatform changed: {Filter}", value);
        DebounceSearch();
    }

    partial void OnFilterGenreChanged(string? value)
    {
        _logger.LogTrace("FilterGenre changed: {Filter}", value);
        DebounceSearch();
    }

    partial void OnSortByChanged(LibrarySearchRequest.Types.SortOption value)
    {
        _logger.LogTrace("SortBy changed: {SortBy}", value);
        DebounceSearch();
        OnPropertyChanged(nameof(SortByIndex));
    }

    partial void OnSortAscendingChanged(bool value)
    {
        _logger.LogTrace("SortAscending changed: {Value}", value);
        DebounceSearch();
    }

    public void ClearFilters()
    {
        _logger.LogDebug("ClearFilters invoked");
        FilterPlatform = null;
        FilterGenre = null;
    }

    public void ClearSearch()
    {
        _logger.LogDebug("ClearSearch invoked");
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
        _logger.LogDebug("TogglePlatformFilter invoked: {Platform}", platform);
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
        _logger.LogTrace("DebounceSearch invoked");
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
        _logger.LogInformation("PerformSearchAsync invoked");
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
            _logger.LogError(ex, "Search failed");
        }
    }

    private void HookPluginCollection()
    {
        _logger.LogDebug("HookPluginCollection invoked");
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
        _logger.LogTrace("AttachPluginsCollection invoked");
        if (_pluginsChangedHandler != null)
        {
            _mainViewModel.Plugins.CollectionChanged -= _pluginsChangedHandler;
        }

        _pluginsChangedHandler = (_, _) => RefreshAvailableImporters();
        _mainViewModel.Plugins.CollectionChanged += _pluginsChangedHandler;
    }

    private void RefreshAvailableImporters()
    {
        _logger.LogDebug("RefreshAvailableImporters invoked");
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
        _logger.LogTrace("UpdateEmptyState invoked");
        ShowEmptyState = !IsSearching && Results.Count == 0 && !string.IsNullOrWhiteSpace(Query);
    }

    private Task EnqueueAsync(Action action)
    {
        _logger.LogTrace("EnqueueAsync invoked");
        var tcs = new TaskCompletionSource<bool>();
        _dispatcher.TryEnqueue(() =>
        {
            action();
            tcs.SetResult(true);
        });
        return tcs.Task;
    }
}
