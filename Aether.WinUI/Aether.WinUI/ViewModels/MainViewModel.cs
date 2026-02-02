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

namespace Aether.WinUI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly GrpcClientService _grpc;
    private readonly BackendManager _backend;
    private readonly DispatcherQueue _dispatcherQueue;

    // Navigation Event
    public event EventHandler<string>? NavigateToGameDetailRequested;

    [RelayCommand]
    public void GoToGameDetail(string gameId)
    {
        NavigateToGameDetailRequested?.Invoke(this, gameId);
    }

    [ObservableProperty] private ObservableCollection<GameViewModel> games = new();
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

    public MainViewModel(GrpcClientService grpc, BackendManager backend)
    {
        _grpc = grpc;
        _backend = backend;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

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
                StatusMessage = "Ready";
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
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
