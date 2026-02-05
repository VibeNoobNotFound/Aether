using global::Aether.Protos;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Aether.WinUI.Models;

public partial class GameViewModel : ObservableObject
{
    [ObservableProperty] private string id = "";
    [ObservableProperty] private string title = "";
    [ObservableProperty] private string platform = "";
    [ObservableProperty] private string externalId = "";
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
    [ObservableProperty] private string shortDescription = "";
    [ObservableProperty] private double metacriticScore;
    [ObservableProperty] private double userScore;
    [ObservableProperty] private int reviewCount;
    [ObservableProperty] private long totalPlaytimeSeconds;
    [ObservableProperty] private long lastPlayedUnix;
    [ObservableProperty] private DateTimeOffset? lastPlayed;

    // Images
    [ObservableProperty] private string? coverImageUrl;
    [ObservableProperty] private string? backgroundImageUrl;
    [ObservableProperty] private string? logoImageUrl;
    [ObservableProperty] private string[] screenshots = Array.Empty<string>();
    [ObservableProperty] private string[] videos = Array.Empty<string>();

    // Collections
    [ObservableProperty] private string[] genres = Array.Empty<string>();
    [ObservableProperty] private string[] tags = Array.Empty<string>();
    [ObservableProperty] private string[] categories = Array.Empty<string>();

    // Advanced Metadata
    [ObservableProperty] private string? steamId;
    [ObservableProperty] private string? launchArguments;
    [ObservableProperty] private int playCount;
    [ObservableProperty] private long releaseDateUnix;
    [ObservableProperty] private bool hasAchievements;
    [ObservableProperty] private int achievementCount;
    [ObservableProperty] private bool hasMultiplayer;
    [ObservableProperty] private bool hasSinglePlayer;
    [ObservableProperty] private bool hasCloudSaves;
    [ObservableProperty] private string? minimumRequirements;
    [ObservableProperty] private string? recommendedRequirements;
    [ObservableProperty] private string[] supportedLanguages = Array.Empty<string>();

    // Computed Properties
    public string FormattedPlaytime => TimeSpan.FromSeconds(TotalPlaytimeSeconds).ToString(@"hh\:mm");
    public string FormattedReleaseDate => ReleaseDateUnix > 0 ? DateTimeOffset.FromUnixTimeSeconds(ReleaseDateUnix).ToString("MMM dd, yyyy") : "Unknown";
    public string FormattedLastPlayed => LastPlayed?.ToString("MMM dd, yyyy") ?? "Never";
    public string GenresText => string.Join(", ", Genres ?? Array.Empty<string>());
    public string TagsText => string.Join(", ", Tags ?? Array.Empty<string>());
    public string CategoriesText => string.Join(", ", Categories ?? Array.Empty<string>());
    public string SupportedLanguagesText => string.Join(", ", SupportedLanguages ?? Array.Empty<string>());
    public string MinimumRequirementsDisplay => string.IsNullOrWhiteSpace(MinimumRequirements) ? "Unknown" : MinimumRequirements!;
    public string RecommendedRequirementsDisplay => string.IsNullOrWhiteSpace(RecommendedRequirements) ? "Unknown" : RecommendedRequirements!;
    public string SupportedLanguagesDisplay => string.IsNullOrWhiteSpace(SupportedLanguagesText) ? "Unknown" : SupportedLanguagesText;
    public string AchievementsDisplay => HasAchievements ? $"{AchievementCount} achievements" : "No achievements";

    public Uri? CoverImageUri => !string.IsNullOrEmpty(CoverImageUrl) && Uri.TryCreate(CoverImageUrl, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
    public Uri? BackgroundImageUri => !string.IsNullOrEmpty(BackgroundImageUrl) && Uri.TryCreate(BackgroundImageUrl, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
    public Uri? LogoImageUri => !string.IsNullOrEmpty(LogoImageUrl) && Uri.TryCreate(LogoImageUrl, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;

    public bool HasLogo => !string.IsNullOrEmpty(LogoImageUrl);
    public string? DisplayBackgroundImageUrl => !string.IsNullOrEmpty(BackgroundImageUrl) ? BackgroundImageUrl : CoverImageUrl;
    public string DisplayDescription => !string.IsNullOrEmpty(Description) ? Description : ShortDescription;
    public string DescriptionPlain => StripHtml(DisplayDescription);
    public bool HasMetacriticScore => MetacriticScore > 0;
    public bool HasUserScore => UserScore > 0;
    public bool HasReleaseDate => ReleaseDateUnix > 0;
    public bool HasTags => Tags != null && Tags.Length > 0;
    public bool HasCategories => Categories != null && Categories.Length > 0;

    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", string.Empty);
        text = text.Replace("&nbsp;", " ")
                   .Replace("&amp;", "&")
                   .Replace("&lt;", "<")
                   .Replace("&gt;", ">")
                   .Replace("&quot;", "\"")
                   .Replace("&#39;", "'");
        return text.Trim();
    }

    public static GameViewModel FromProto(Game proto)
    {
        return new GameViewModel
        {
            Id = proto.Id,
            Title = proto.Title,
            Platform = proto.Platform,
            ExternalId = proto.ExternalId,
            ExecutablePath = proto.ExecutablePath,
            InstallPath = proto.InstallPath,
            IsFavorite = proto.IsFavorite,
            IsInstalled = proto.IsInstalled,
            Developer = proto.Developer,
            Publisher = proto.Publisher,
            Description = proto.Description,
            ShortDescription = proto.ShortDescription,
            MetacriticScore = proto.MetacriticScore,
            UserScore = proto.UserScore,
            ReviewCount = proto.ReviewCount,
            TotalPlaytimeSeconds = proto.TotalPlaytimeSeconds,
            LastPlayedUnix = proto.LastPlayedUnix,
            LastPlayed = proto.LastPlayedUnix > 0 ? DateTimeOffset.FromUnixTimeSeconds(proto.LastPlayedUnix) : null,
            CoverImageUrl = proto.CoverImageUrl,
            BackgroundImageUrl = proto.BackgroundImageUrl,
            LogoImageUrl = proto.LogoImageUrl,
            Genres = proto.Genres.ToArray(),
            Tags = proto.Tags.ToArray(),
            Categories = proto.Categories.ToArray(),
            Screenshots = proto.Screenshots.ToArray(),
            Videos = proto.Videos.ToArray(),
            SteamId = proto.SteamId,
            LaunchArguments = proto.LaunchArguments,
            PlayCount = proto.PlayCount,
            ReleaseDateUnix = proto.ReleaseDateUnix,
            HasAchievements = proto.HasAchievements,
            AchievementCount = proto.AchievementCount,
            HasMultiplayer = proto.HasMultiplayer,
            HasSinglePlayer = proto.HasSinglePlayer,
            HasCloudSaves = proto.HasCloudSaves,
            MinimumRequirements = string.IsNullOrWhiteSpace(proto.MinimumRequirements) ? null : proto.MinimumRequirements,
            RecommendedRequirements = string.IsNullOrWhiteSpace(proto.RecommendedRequirements) ? null : proto.RecommendedRequirements,
            SupportedLanguages = proto.SupportedLanguages.ToArray()
        };
    }
}
