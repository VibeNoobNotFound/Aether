using global::Aether.Protos;
using Aether.WinUI.Models;
using Aether.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grpc.Core;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aether.WinUI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly GrpcClientService _grpc;
    private readonly BackendManager _backend;
    private readonly DispatcherQueue _dispatcherQueue;

    // Navigation Events
    public event EventHandler<string>? NavigateToGameDetailRequested;
    public event EventHandler? NavigateToLibraryRequested;

    [RelayCommand]
    public void GoToGameDetail(string gameId)
    {
        _logger.LogInformation("GoToGameDetail: {GameId}", gameId);
        NavigateToGameDetailRequested?.Invoke(this, gameId);
    }

    [RelayCommand]
    public void GoToLibrary()
    {
        _logger.LogInformation("GoToLibrary invoked");
        CurrentScreen = AppScreen.Library;
        NavigateToLibraryRequested?.Invoke(this, EventArgs.Empty);
    }

    [ObservableProperty] private ObservableCollection<GameViewModel> games = new();
    [ObservableProperty] private ObservableCollection<GameViewModel> carouselGames = new();
    public bool IsLibraryEmpty => Games.Count == 0;

    [ObservableProperty] private ObservableCollection<CollectionViewModel> collections = new();
    [ObservableProperty] private ObservableCollection<PluginViewModel> plugins = new();
    [ObservableProperty] private ObservableCollection<HomeCollectionViewModel> homeCollections = new();
    [ObservableProperty] private ObservableCollection<NewsItemViewModel> generalNews = new();
    [ObservableProperty] private string? windowBackgroundImageUrl;
    [ObservableProperty] private double windowBackgroundOpacity = 0.5;

    [ObservableProperty] private AppScreen currentScreen = AppScreen.Home;
    [ObservableProperty] private string searchQuery = "";

    [ObservableProperty] private bool isBackendRunning;
    [ObservableProperty] private string statusMessage = "";

    // Update State
    [ObservableProperty] private bool isUpdateAvailable;
    [ObservableProperty] private string updateVersion = "";

    // Streaming Cancellation Tokens
    private CancellationTokenSource? _gameStateCts;

    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(GrpcClientService grpc, BackendManager backend, ILogger<MainViewModel> logger)
    {
        _grpc = grpc;
        _backend = backend;
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _logger.LogInformation("MainViewModel initialized");

        // Bind backend state
        _backend.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(BackendManager.ConnectionState))
            {
                OnPropertyChanged(nameof(ConnectionState));
                if (_backend.ConnectionState == ConnectionState.Connected)
                {
                    _ = InitializeDataAsync();
                }
            }
        };
    }

    public ConnectionState ConnectionState => _backend.ConnectionState;

    public async Task StartBackendAsync()
    {
        _logger.LogInformation("StartBackendAsync invoked");
        await _backend.StartAsync();
    }

    private async Task InitializeDataAsync()
    {
        _logger.LogInformation("InitializeDataAsync invoked");
        await RefreshLibraryAsync();
        _ = RefreshCollectionsAsync();
        _ = LoadCarouselGamesAsync();
        _ = FetchPluginsAsync();
        _ = LoadGeneralNewsAsync();
        _ = SubscribeToGameStateAsync();
    }

    [RelayCommand]
    public async Task RefreshLibraryAsync()
    {
        _logger.LogInformation("RefreshLibraryAsync invoked");
        try
        {
            StatusMessage = "Refreshing library...";

            // 1. Get all games
            var call = _grpc.Client.GetLibrary(new Empty());
            var tempGames = new ObservableCollection<GameViewModel>();

            await foreach (var gameProto in call.ResponseStream.ReadAllAsync())
            {
                tempGames.Add(GameViewModel.FromProto(gameProto));
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                Games = tempGames;
                OnPropertyChanged(nameof(IsLibraryEmpty));
                UpdateHomeCollections();
                StatusMessage = "Ready";
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError(ex, "RefreshLibraryAsync failed");
        }
    }

    public async Task LoadCarouselGamesAsync()
    {
        _logger.LogInformation("LoadCarouselGamesAsync invoked");
        try
        {
            var call = _grpc.Client.GetCarouselGames(new Empty());
            var tempCarousel = new ObservableCollection<GameViewModel>();

            await foreach (var gameProto in call.ResponseStream.ReadAllAsync())
            {
                tempCarousel.Add(GameViewModel.FromProto(gameProto));
            }

            _dispatcherQueue.TryEnqueue(() =>
           {
               CarouselGames = tempCarousel;
           });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading carousel games");
        }
    }

    public async Task<CarouselConfig?> LoadCarouselConfigAsync()
    {
        _logger.LogInformation("LoadCarouselConfigAsync invoked");
        try
        {
            return await _grpc.Client.GetCarouselConfigAsync(new Empty());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading carousel config");
            return null;
        }
    }

    public async Task SaveCarouselConfigAsync(CarouselConfig config)
    {
        _logger.LogInformation("SaveCarouselConfigAsync invoked");
        try
        {
            await _grpc.Client.SetCarouselConfigAsync(config);
            await LoadCarouselGamesAsync(); // Refresh locally
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving carousel config");
        }
    }

    [RelayCommand]
    public async Task ToggleFavorite(string gameId)
    {
        _logger.LogInformation("ToggleFavorite invoked: {GameId}", gameId);
        try
        {
            await _grpc.Client.ToggleFavoriteAsync(new GameId { Id = gameId });
            // Update local state
            var game = Games.FirstOrDefault(g => g.Id == gameId);
            if (game != null)
            {
                game.IsFavorite = !game.IsFavorite;
                UpdateHomeCollections();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle favorite");
        }
    }

    [RelayCommand]
    public async Task OpenGameLocation(string gameId)
    {
        _logger.LogInformation("OpenGameLocation invoked: {GameId}", gameId);
        try
        {
            await _grpc.Client.OpenGameLocationAsync(new GameId { Id = gameId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open game location");
        }
    }

    [RelayCommand]
    public async Task RemoveGame(string gameId)
    {
        _logger.LogInformation("RemoveGame invoked: {GameId}", gameId);
        try
        {
            await _grpc.Client.RemoveGameAsync(new GameId { Id = gameId });
            var game = Games.FirstOrDefault(g => g.Id == gameId);
            if (game != null)
            {
                Games.Remove(game);
                OnPropertyChanged(nameof(IsLibraryEmpty));
                UpdateHomeCollections();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove game");
        }
    }

    private async Task FetchPluginsAsync()
    {
        _logger.LogInformation("FetchPluginsAsync invoked");
        try
        {
            var response = await _grpc.Client.GetPluginsAsync(new Empty());
            var list = response.Plugins.Select(PluginViewModel.FromProto).ToList();

            _dispatcherQueue.TryEnqueue(() =>
            {
                Plugins = new ObservableCollection<PluginViewModel>(list);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch plugins");
        }
    }

    [RelayCommand]
    public async Task ScanLibraryAsync()
    {
        _logger.LogInformation("ScanLibraryAsync invoked");
        try
        {
            StatusMessage = "Scanning library...";
            var request = new ScanRequest { ForceRefresh = true };
            var call = _grpc.Client.ScanLibrary(request);

            await foreach (var progress in call.ResponseStream.ReadAllAsync())
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    StatusMessage = $"Scanning: {progress.CurrentGame} ({progress.ProgressPercentage:F0}%)";
                    if (progress.FoundGame != null)
                    {
                        var newGame = GameViewModel.FromProto(progress.FoundGame);
                        // Update or add
                        var existing = Games.FirstOrDefault(g => g.Id == newGame.Id);
                        if (existing != null)
                        {
                            var index = Games.IndexOf(existing);
                            Games[index] = newGame;
                        }
                        else
                        {
                            Games.Add(newGame);
                        }
                        UpdateHomeCollections();
                    }
                });
            }

            StatusMessage = "Scan complete";
            UpdateHomeCollections();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
            _logger.LogError(ex, "ScanLibraryAsync failed");
        }
    }

    private async Task SubscribeToGameStateAsync()
    {
        _logger.LogInformation("SubscribeToGameStateAsync invoked");
        _gameStateCts?.Cancel();
        _gameStateCts = new CancellationTokenSource();

        try
        {
            var call = _grpc.Client.SubscribeToGameState(new Empty(), cancellationToken: _gameStateCts.Token);

            await foreach (var update in call.ResponseStream.ReadAllAsync(_gameStateCts.Token))
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    var game = Games.FirstOrDefault(g => g.Id == update.GameId);
                    if (game != null)
                    {
                        game.State = update.State;
                    }
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            // Reconnect logic could go here
            _logger.LogError(ex, "SubscribeToGameStateAsync failed");
        }
    }

    [RelayCommand]
    public async Task LaunchGameAsync(string gameId)
    {
        _logger.LogInformation("LaunchGameAsync invoked: {GameId}", gameId);
        try
        {
            var game = Games.FirstOrDefault(g => g.Id == gameId);
            if (game == null) return;

            // Optimistic update
            game.State = GameState.Launching;

            var response = await _grpc.Client.LaunchGameAsync(new LaunchRequest { GameId = gameId });

            if (!response.Success)
            {
                StatusMessage = $"Launch failed: {response.Message}";
                game.State = GameState.Stopped; // Revert
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Launch error: {ex.Message}";
            _logger.LogError(ex, "LaunchGameAsync failed");
        }
    }

    [RelayCommand]
    public async Task StopGameAsync(string gameId)
    {
        _logger.LogInformation("StopGameAsync invoked: {GameId}", gameId);
        try
        {
            await _grpc.Client.StopGameAsync(new GameId { Id = gameId });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Stop error: {ex.Message}";
            _logger.LogError(ex, "StopGameAsync failed");
        }
    }
    public async Task RefreshCollectionsAsync()
    {
        _logger.LogInformation("RefreshCollectionsAsync invoked");
        try
        {
            // GetCollections is a standard RPC, not streaming
            var response = await _grpc.Client.GetCollectionsAsync(new Empty());
            var list = response.Collections.Select(CollectionViewModel.FromProto).ToList();

            _dispatcherQueue.TryEnqueue(() =>
            {
                Collections = new ObservableCollection<CollectionViewModel>(list.OrderBy(c => c.SortOrder));
                UpdateHomeCollections();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh collections");
        }
    }

    public async Task LoadGeneralNewsAsync()
    {
        _logger.LogInformation("LoadGeneralNewsAsync invoked");
        var news = await FetchGeneralNewsAsync();
        _dispatcherQueue.TryEnqueue(() =>
        {
            GeneralNews = new ObservableCollection<NewsItemViewModel>(news);
        });
    }

    private void UpdateHomeCollections()
    {
        _logger.LogDebug("UpdateHomeCollections invoked");
        var visible = Collections
            .Where(c => c.IsVisible)
            .OrderBy(c => c.SortOrder)
            .ToList();

        var rows = new ObservableCollection<HomeCollectionViewModel>();
        foreach (var collection in visible)
        {
            var games = GetGamesForCollection(collection);
            if (games.Count > 0)
            {
                rows.Add(new HomeCollectionViewModel(collection, games));
            }
        }

        HomeCollections = rows;
    }

    private ObservableCollection<GameViewModel> GetGamesForCollection(CollectionViewModel collection)
    {
        _logger.LogTrace("GetGamesForCollection: {CollectionId}", collection.Id);
        if (!string.IsNullOrWhiteSpace(collection.PlatformFilter))
        {
            var filter = collection.PlatformFilter.Trim().ToLowerInvariant();
            var byPlatform = Games.Where(g => g.Platform?.ToLowerInvariant().Contains(filter) == true);
            return new ObservableCollection<GameViewModel>(byPlatform);
        }

        if (collection.Type == CollectionType.CollectionFavorites)
        {
            return new ObservableCollection<GameViewModel>(Games.Where(g => g.IsFavorite));
        }

        if (collection.Type == CollectionType.CollectionRecentlyPlayed)
        {
            var oneMonthAgo = DateTimeOffset.UtcNow.AddDays(-30);
            var recent = Games
                .Where(g => g.LastPlayed.HasValue && g.LastPlayed.Value > oneMonthAgo)
                .OrderByDescending(g => g.LastPlayed);
            return new ObservableCollection<GameViewModel>(recent);
        }

        if (collection.Type == CollectionType.CollectionCustom && collection.GameIds.Count > 0)
        {
            var idSet = collection.GameIds.ToHashSet();
            var custom = Games.Where(g => idSet.Contains(g.Id));
            return new ObservableCollection<GameViewModel>(custom);
        }

        return new ObservableCollection<GameViewModel>();
    }

    public async Task CreateCollectionAsync(string name, string icon, IEnumerable<string> gameIds)
    {
        _logger.LogInformation("CreateCollectionAsync invoked: {Name}", name);
        try
        {
            // Fix: IconName
            var response = await _grpc.Client.CreateCollectionAsync(new CreateCollectionRequest { Name = name, IconName = icon });

            if (gameIds != null && gameIds.Any())
            {
                foreach (var id in gameIds)
                {
                    // Fix: CollectionGameAction
                    await _grpc.Client.AddGameToCollectionAsync(new CollectionGameAction { CollectionId = response.Id, GameId = id });
                }
            }

            await RefreshCollectionsAsync();
            UpdateHomeCollections();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create collection");
        }
    }

    public async Task DeleteCollectionAsync(int collectionId)
    {
        _logger.LogInformation("DeleteCollectionAsync invoked: {CollectionId}", collectionId);
        try
        {
            await _grpc.Client.DeleteCollectionAsync(new CollectionId { Id = collectionId });
            var col = Collections.FirstOrDefault(c => c.Id == collectionId);
            if (col != null) Collections.Remove(col);
            UpdateHomeCollections();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete collection");
        }
    }

    public async Task ToggleCollectionVisibilityAsync(CollectionViewModel collection)
    {
        _logger.LogInformation("ToggleCollectionVisibilityAsync invoked: {CollectionId}", collection.Id);
        try
        {
            collection.IsVisible = !collection.IsVisible;
            // Fix: IconName
            await _grpc.Client.UpdateCollectionAsync(new UpdateCollectionRequest
            {
                Id = collection.Id,
                Name = collection.Name,
                IconName = collection.Icon,
                IsVisible = collection.IsVisible
            });
            UpdateHomeCollections();
        }
        catch (Exception ex)
        {
            collection.IsVisible = !collection.IsVisible; // Revert
            _logger.LogError(ex, "Failed to toggle visibility");
        }
    }

    public async Task ReorderCollectionsAsync(IEnumerable<int> orderedIds)
    {
        _logger.LogInformation("ReorderCollectionsAsync invoked");
        try
        {
            var request = new ReorderCollectionsRequest();
            request.CollectionIds.AddRange(orderedIds);
            await _grpc.Client.ReorderCollectionsAsync(request);
            UpdateHomeCollections();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder collections");
        }
    }

    public async Task<CanLaunchResponse?> CanLaunchGameAsync(string gameId)
    {
        _logger.LogInformation("CanLaunchGameAsync invoked: {GameId}", gameId);
        try
        {
            return await _grpc.Client.CanLaunchGameAsync(new GameId { Id = gameId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check can launch");
            return null;
        }
    }

    public async Task<ActiveProcessesResponse?> GetActiveProcessesAsync(string gameId)
    {
        _logger.LogInformation("GetActiveProcessesAsync invoked: {GameId}", gameId);
        try
        {
            return await _grpc.Client.GetActiveProcessesAsync(new GameId { Id = gameId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active processes");
            return null;
        }
    }

    public async Task<LibraryStatsResponse?> GetLibraryStatsAsync()
    {
        _logger.LogInformation("GetLibraryStatsAsync invoked");
        try
        {
            return await _grpc.Client.GetLibraryStatsAsync(new Empty());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get library stats");
            return null;
        }
    }

    public async Task<IReadOnlyList<NewsItemViewModel>> FetchGeneralNewsAsync()
    {
        _logger.LogInformation("FetchGeneralNewsAsync invoked");
        try
        {
            var response = await _grpc.Client.GetGeneralNewsAsync(new Empty());
            return response.News.Select(NewsItemViewModel.FromProto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch general news");
            return Array.Empty<NewsItemViewModel>();
        }
    }

    public async Task<IReadOnlyList<NewsItemViewModel>> FetchGameNewsAsync(string gameId)
    {
        _logger.LogInformation("FetchGameNewsAsync invoked: {GameId}", gameId);
        try
        {
            var response = await _grpc.Client.GetGameNewsAsync(new GameId { Id = gameId });
            return response.News.Select(NewsItemViewModel.FromProto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch game news");
            return Array.Empty<NewsItemViewModel>();
        }
    }
}
