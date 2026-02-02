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
        NavigateToGameDetailRequested?.Invoke(this, gameId);
    }

    [RelayCommand]
    public void GoToLibrary()
    {
        CurrentScreen = AppScreen.Library;
        NavigateToLibraryRequested?.Invoke(this, EventArgs.Empty);
    }

    [ObservableProperty] private ObservableCollection<GameViewModel> games = new();
    [ObservableProperty] private ObservableCollection<GameViewModel> carouselGames = new();
    public bool IsLibraryEmpty => Games.Count == 0;

    [ObservableProperty] private ObservableCollection<CollectionViewModel> collections = new();
    [ObservableProperty] private ObservableCollection<PluginViewModel> plugins = new();

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
        await _backend.StartAsync();
    }

    private async Task InitializeDataAsync()
    {
        await RefreshLibraryAsync();
        _ = LoadCarouselGamesAsync();
        _ = FetchPluginsAsync();
        _ = SubscribeToGameStateAsync();
    }

    [RelayCommand]
    public async Task RefreshLibraryAsync()
    {
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
                StatusMessage = "Ready";
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    public async Task LoadCarouselGamesAsync()
    {
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
            System.Diagnostics.Debug.WriteLine($"Error loading carousel games: {ex.Message}");
        }
    }

    public async Task<CarouselConfig?> LoadCarouselConfigAsync()
    {
        try
        {
            return await _grpc.Client.GetCarouselConfigAsync(new Empty());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading carousel config: {ex.Message}");
            return null;
        }
    }

    public async Task SaveCarouselConfigAsync(CarouselConfig config)
    {
        try
        {
            await _grpc.Client.SetCarouselConfigAsync(config);
            await LoadCarouselGamesAsync(); // Refresh locally
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving carousel config: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ToggleFavorite(string gameId)
    {
        try
        {
            await _grpc.Client.ToggleFavoriteAsync(new GameId { Id = gameId });
            // Update local state
            var game = Games.FirstOrDefault(g => g.Id == gameId);
            if (game != null)
            {
                game.IsFavorite = !game.IsFavorite;
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
        try
        {
            await _grpc.Client.RemoveGameAsync(new GameId { Id = gameId });
            var game = Games.FirstOrDefault(g => g.Id == gameId);
            if (game != null)
            {
                Games.Remove(game);
                OnPropertyChanged(nameof(IsLibraryEmpty));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove game");
        }
    }

    private async Task FetchPluginsAsync()
    {
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
            // Log error
            System.Diagnostics.Debug.WriteLine($"Failed to fetch plugins: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ScanLibraryAsync()
    {
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
                    }
                });
            }

            StatusMessage = "Scan complete";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
    }

    private async Task SubscribeToGameStateAsync()
    {
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
        catch (Exception)
        {
            // Reconnect logic could go here
        }
    }

    [RelayCommand]
    public async Task LaunchGameAsync(string gameId)
    {
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
        }
    }

    [RelayCommand]
    public async Task StopGameAsync(string gameId)
    {
        try
        {
            await _grpc.Client.StopGameAsync(new GameId { Id = gameId });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Stop error: {ex.Message}";
        }
    }
}
