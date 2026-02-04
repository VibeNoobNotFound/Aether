using Aether.WinUI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using Aether.Protos;

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

    [ObservableProperty] private bool isCheckingLaunch = true;
    [ObservableProperty] private bool canLaunch;
    [ObservableProperty] private string? launchReason;
    [ObservableProperty] private string? launchMethod;

    [ObservableProperty] private ObservableCollection<NewsItemViewModel> gameNews = new();
    public bool HasGameNews => GameNews.Count > 0;

    [ObservableProperty] private ObservableCollection<MediaItemViewModel> mediaItems = new();

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

        BuildMediaItems();
        await RefreshLaunchStateAsync(gameId);
        await RefreshNewsAsync(gameId);
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

    public string GetLaunchButtonText()
    {
        if (string.IsNullOrWhiteSpace(LaunchMethod)) return "Play Now";
        return LaunchMethod.ToLowerInvariant() switch
        {
            "steam" => "Play on Steam",
            "epic_games" => "Play on Epic",
            "app_store" => "Play",
            "direct" => "Launch",
            _ => "Play Now"
        };
    }

    private async Task RefreshLaunchStateAsync(string gameId)
    {
        IsCheckingLaunch = true;
        var response = await _mainViewModel.CanLaunchGameAsync(gameId);
        if (response != null)
        {
            CanLaunch = response.CanLaunch;
            LaunchReason = response.Reason;
            LaunchMethod = response.LaunchMethod;
        }
        else
        {
            CanLaunch = false;
            LaunchReason = "Unable to check";
            LaunchMethod = null;
        }
        IsCheckingLaunch = false;
    }

    private async Task RefreshNewsAsync(string gameId)
    {
        var news = await _mainViewModel.FetchGameNewsAsync(gameId);
        GameNews = new ObservableCollection<NewsItemViewModel>(news);
        OnPropertyChanged(nameof(HasGameNews));
    }

    private void BuildMediaItems()
    {
        var list = new ObservableCollection<MediaItemViewModel>();
        if (SelectedGame != null)
        {
            if (SelectedGame.Videos != null)
            {
                foreach (var video in SelectedGame.Videos)
                {
                    if (!string.IsNullOrWhiteSpace(video))
                    {
                        list.Add(new MediaItemViewModel(video, true));
                    }
                }
            }

            if (SelectedGame.Screenshots != null)
            {
                foreach (var shot in SelectedGame.Screenshots)
                {
                    if (!string.IsNullOrWhiteSpace(shot))
                    {
                        list.Add(new MediaItemViewModel(shot, false));
                    }
                }
            }
        }

        MediaItems = list;
    }
}
