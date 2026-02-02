using global::Aether.Protos;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Aether.WinUI.Models;

public partial class GameViewModel : ObservableObject
{
    [ObservableProperty] private string id = "";
    [ObservableProperty] private string title = "";
    [ObservableProperty] private string platform = "";
    [ObservableProperty] private string executablePath = "";
    [ObservableProperty] private string installPath = "";

    // State
    [ObservableProperty] private GameState state = GameState.Stopped;
    [ObservableProperty] private bool isFavorite;
    [ObservableProperty] private bool isInstalled;

    // Metadata
    [ObservableProperty] private string developer = "";
    [ObservableProperty] private string publisher = "";
    [ObservableProperty] private string description = "";
    [ObservableProperty] private double metacriticScore;
    [ObservableProperty] private long totalPlaytimeSeconds;
    [ObservableProperty] private long lastPlayedUnix;
    [ObservableProperty] private DateTimeOffset? lastPlayed;

    // Images
    [ObservableProperty] private string? coverImageUrl;
    [ObservableProperty] private string? backgroundImageUrl;
    [ObservableProperty] private string? logoImageUrl;

    // Collections
    [ObservableProperty] private string[] genres = Array.Empty<string>();

    // Computed Properties
    public string FormattedPlaytime => TimeSpan.FromSeconds(TotalPlaytimeSeconds).ToString(@"hh\:mm");
    public Uri? CoverImageUri => !string.IsNullOrEmpty(CoverImageUrl) ? new Uri(CoverImageUrl) : null;
    public Uri? BackgroundImageUri => !string.IsNullOrEmpty(BackgroundImageUrl) ? new Uri(BackgroundImageUrl) : null;
    public Uri? LogoImageUri => !string.IsNullOrEmpty(LogoImageUrl) ? new Uri(LogoImageUrl) : null;

    public static GameViewModel FromProto(Game proto)
    {
        return new GameViewModel
        {
            Id = proto.Id,
            Title = proto.Title,
            Platform = proto.Platform,
            ExecutablePath = proto.ExecutablePath,
            InstallPath = proto.InstallPath,
            IsFavorite = proto.IsFavorite,
            IsInstalled = proto.IsInstalled,
            Developer = proto.Developer,
            Publisher = proto.Publisher,
            Description = proto.Description,
            MetacriticScore = proto.MetacriticScore,
            TotalPlaytimeSeconds = proto.TotalPlaytimeSeconds,
            LastPlayedUnix = proto.LastPlayedUnix,
            LastPlayed = proto.LastPlayedUnix > 0 ? DateTimeOffset.FromUnixTimeSeconds(proto.LastPlayedUnix) : null,
            CoverImageUrl = proto.CoverImageUrl,
            BackgroundImageUrl = proto.BackgroundImageUrl,
            LogoImageUrl = proto.LogoImageUrl,
            Genres = proto.Genres.ToArray()
        };
    }
}
