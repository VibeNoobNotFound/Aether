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
        _logger.LogDebug("GameDetailViewModel initialized");
    }

    public MainViewModel MainViewModel => _mainViewModel;

    [ObservableProperty] private bool isCheckingLaunch = true;
    [ObservableProperty] private bool canLaunch;
    [ObservableProperty] private string? launchReason;
    [ObservableProperty] private string? launchMethod;

    [ObservableProperty] private ObservableCollection<NewsItemViewModel> gameNews = new();
    public bool HasGameNews => GameNews.Count > 0;

    [ObservableProperty] private ObservableCollection<MediaItemViewModel> mediaItems = new();

    [ObservableProperty] private bool isDescriptionExpanded;

    public bool HasLongDescription
        => (SelectedGame?.DescriptionPlain?.Length ?? 0) > 400;

    public int DescriptionMaxLines => IsDescriptionExpanded ? 1000 : 6;

    public bool ShowReadMore => HasLongDescription && !IsDescriptionExpanded;

    public async Task LoadGameAsync(string gameId)
    {
        _logger.LogInformation("LoadGameAsync: {GameId}", gameId);
        System.Diagnostics.Debug.WriteLine($"[GameDetailViewModel] Loading game: {gameId}");
        System.Diagnostics.Debug.WriteLine($"[GameDetailViewModel] MainViewModel.Games count: {_mainViewModel.Games.Count}");

        SelectedGame = _mainViewModel.Games.FirstOrDefault(g => g.Id == gameId);

        if (SelectedGame == null)
        {
            System.Diagnostics.Debug.WriteLine($"[GameDetailViewModel] Game NOT FOUND in cache!");
            _logger.LogWarning("Game not found in cache: {GameId}", gameId);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[GameDetailViewModel] Game loaded: {SelectedGame.Title}");
            _logger.LogDebug("Game loaded: {GameTitle}", SelectedGame.Title);
        }

        BuildMediaItems();
        await RefreshLaunchStateAsync(gameId);
        await RefreshNewsAsync(gameId);
    }

    partial void OnSelectedGameChanged(GameViewModel? value)
    {
        _logger.LogDebug("Selected game changed: {GameId}", value?.Id);
        IsDescriptionExpanded = false;
        OnPropertyChanged(nameof(HasLongDescription));
        OnPropertyChanged(nameof(DescriptionMaxLines));
        OnPropertyChanged(nameof(ShowReadMore));
    }

    partial void OnIsDescriptionExpandedChanged(bool value)
    {
        _logger.LogDebug("Description expanded changed: {IsExpanded}", value);
        OnPropertyChanged(nameof(DescriptionMaxLines));
        OnPropertyChanged(nameof(ShowReadMore));
    }

    [RelayCommand]
    public async Task LaunchGame(string gameId)
    {
        _logger.LogInformation("LaunchGame command: {GameId}", gameId);
        await _mainViewModel.LaunchGameCommand.ExecuteAsync(gameId);
    }

    [RelayCommand]
    public async Task ToggleFavorite(string gameId)
    {
        _logger.LogInformation("ToggleFavorite command: {GameId}", gameId);
        await _mainViewModel.ToggleFavoriteCommand.ExecuteAsync(gameId);
    }

    [RelayCommand]
    private void ExpandDescription()
    {
        _logger.LogInformation("ExpandDescription invoked");
        IsDescriptionExpanded = true;
    }

    public string GetLaunchButtonText()
    {
        _logger.LogTrace("GetLaunchButtonText method: {LaunchMethod}", LaunchMethod);
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
        _logger.LogDebug("RefreshLaunchStateAsync: {GameId}", gameId);
        IsCheckingLaunch = true;
        var response = await _mainViewModel.CanLaunchGameAsync(gameId);
        if (response != null)
        {
            CanLaunch = response.CanLaunch;
            LaunchReason = response.Reason;
            LaunchMethod = response.LaunchMethod;
            _logger.LogDebug("Launch state: canLaunch={CanLaunch} method={LaunchMethod}", CanLaunch, LaunchMethod);
        }
        else
        {
            CanLaunch = false;
            LaunchReason = "Unable to check";
            LaunchMethod = null;
            _logger.LogWarning("Launch state unavailable for {GameId}", gameId);
        }
        IsCheckingLaunch = false;
    }

    private async Task RefreshNewsAsync(string gameId)
    {
        _logger.LogDebug("RefreshNewsAsync: {GameId}", gameId);
        var news = await _mainViewModel.FetchGameNewsAsync(gameId);
        GameNews = new ObservableCollection<NewsItemViewModel>(news);
        OnPropertyChanged(nameof(HasGameNews));
        _logger.LogDebug("Game news loaded: {Count}", GameNews.Count);
    }

    private void BuildMediaItems()
    {
        _logger.LogDebug("BuildMediaItems invoked");
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
        _logger.LogDebug("Media items count: {Count}", MediaItems.Count);
    }
}
