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
        // For now, load from MainViewModel cache
        // In future, fetch fresh details from gRPC
        SelectedGame = _mainViewModel.Games.FirstOrDefault(g => g.Id == gameId);
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
