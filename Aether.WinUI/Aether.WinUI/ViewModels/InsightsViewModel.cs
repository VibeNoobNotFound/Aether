using Aether.WinUI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Aether.WinUI.ViewModels;

public partial class InsightsViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;
    private readonly ILogger<InsightsViewModel> _logger;

    [ObservableProperty] private bool isLoading = true;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private int selectedTab;
    [ObservableProperty] private int totalGames;
    [ObservableProperty] private double totalHours;
    [ObservableProperty] private int totalSessions;
    [ObservableProperty] private int activeDays;
    [ObservableProperty] private string topGenre = "Unknown";
    [ObservableProperty] private int maxGenreCount;

    public ObservableCollection<GenreStatViewModel> TopGenres { get; } = new();
    public ObservableCollection<GameViewModel> TopGames { get; } = new();

    public InsightsViewModel(MainViewModel mainViewModel, ILogger<InsightsViewModel> logger)
    {
        _mainViewModel = mainViewModel;
        _logger = logger;
        _logger.LogDebug("InsightsViewModel initialized");
    }

    public async Task LoadAsync()
    {
        _logger.LogInformation("InsightsViewModel.LoadAsync invoked");
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var stats = await _mainViewModel.GetLibraryStatsAsync();
            if (stats == null)
            {
                ErrorMessage = "Unable to load stats.";
                _logger.LogWarning("Library stats returned null");
                return;
            }

            TotalGames = stats.TotalGames;
            TotalSessions = stats.TotalSessions;
            ActiveDays = stats.ActiveDayCount;
            TotalHours = Math.Round(stats.TotalPlaytimeSeconds / 3600.0, 1);

            TopGenres.Clear();
            var maxCount = stats.TopGenres.Count > 0 ? stats.TopGenres.Max(g => g.Count) : 0;
            MaxGenreCount = Math.Max(1, maxCount);

            foreach (var genre in stats.TopGenres)
            {
                TopGenres.Add(new GenreStatViewModel(genre.Genre, genre.Count, MaxGenreCount));
            }

            TopGenre = TopGenres.FirstOrDefault()?.Name ?? "Unknown";

            TopGames.Clear();
            var topGames = _mainViewModel.Games
                .OrderByDescending(g => g.TotalPlaytimeSeconds)
                .Take(5)
                .ToList();

            foreach (var game in topGames)
            {
                TopGames.Add(game);
            }

            _logger.LogInformation("InsightsViewModel loaded: games={TotalGames} hours={TotalHours}", TotalGames, TotalHours);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _logger.LogError(ex, "InsightsViewModel.LoadAsync failed");
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public sealed class GenreStatViewModel
{
    private static readonly ILogger<GenreStatViewModel> Logger =
        (Ioc.Default.GetService<ILogger<GenreStatViewModel>>()) ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GenreStatViewModel>.Instance;

    public string Name { get; }
    public int Count { get; }
    public double Percent { get; }

    public GenreStatViewModel(string name, int count, int maxCount)
    {
        Name = name;
        Count = count;
        Percent = maxCount > 0 ? (double)count / maxCount : 0;
        Logger.LogTrace("GenreStatViewModel created: {Name} {Count}", name, count);
    }
}
