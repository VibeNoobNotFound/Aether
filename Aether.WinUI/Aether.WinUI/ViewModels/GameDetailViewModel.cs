using Aether.WinUI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Threading.Tasks;

namespace Aether.WinUI.ViewModels;

public partial class GameDetailViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty] private GameViewModel? selectedGame;

    public GameDetailViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
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
        // TODO: Implement toggle favorite gRPC call
        await Task.CompletedTask;
    }
}
