using Aether.WinUI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aether.WinUI.ViewModels;

public partial class GameDetailViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty] private GameViewModel? selectedGame;

    private readonly ILogger<GameDetailViewModel> _logger;

    public GameDetailViewModel(MainViewModel mainViewModel, ILogger<GameDetailViewModel> logger)
    {
        _mainViewModel = mainViewModel;
        _logger = logger;
    }

    public MainViewModel MainViewModel => _mainViewModel;

    public async Task LoadGameAsync(string gameId)
    {
        System.Diagnostics.Debug.WriteLine($"[GameDetailViewModel] Loading game: {gameId}");
        System.Diagnostics.Debug.WriteLine($"[GameDetailViewModel] MainViewModel.Games count: {_mainViewModel.Games.Count}");

        SelectedGame = _mainViewModel.Games.FirstOrDefault(g => g.Id == gameId);

        if (SelectedGame == null)
        {
            System.Diagnostics.Debug.WriteLine($"[GameDetailViewModel] Game NOT FOUND in cache!");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[GameDetailViewModel] Game loaded: {SelectedGame.Title}");
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task LaunchGame(string gameId)
    {
        await _mainViewModel.LaunchGameCommand.ExecuteAsync(gameId);
    }

    [RelayCommand]
    public async Task ToggleFavorite(string gameId)
    {
        await _mainViewModel.ToggleFavoriteCommand.ExecuteAsync(gameId);
    }
}
