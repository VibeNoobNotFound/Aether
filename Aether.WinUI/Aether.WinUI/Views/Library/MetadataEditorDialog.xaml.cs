using Aether.Protos;
using Aether.WinUI.Models;
using Aether.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Aether.WinUI.Views.Library;

public sealed partial class MetadataEditorDialog : ContentDialog
{
    public MetadataEditorViewModel ViewModel { get; }

    public MetadataEditorDialog(GameViewModel game)
    {
        this.InitializeComponent();
        ViewModel = new MetadataEditorViewModel(game);
        this.PrimaryButtonClick += MetadataEditorDialog_PrimaryButtonClick;
    }

    private async void MetadataEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            await ViewModel.SaveAsync();
        }
        catch (Exception)
        {
            // TODO: args.Cancel = true; // Keep dialog open on error?
        }
        finally
        {
            deferral.Complete();
        }
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var query = SearchBox.Text?.Trim();
        if (string.IsNullOrEmpty(query))
        {
            SearchStatus.Text = "Please enter a search query";
            return;
        }

        SearchProgress.IsActive = true;
        SearchStatus.Text = "Searching...";

        try
        {
            var provider = ProviderComboBox.SelectedIndex switch
            {
                1 => "Steam",
                2 => "IGDB",
                _ => ""
            };

            await ViewModel.SearchMetadataAsync(query, provider);
            if (ViewModel.SearchResults.Count == 0)
            {
                SearchStatus.Text = "No results found";
            }
            else
            {
                SearchStatus.Text = $"Found {ViewModel.SearchResults.Count} result(s)";
            }
        }
        catch (Exception ex)
        {
            SearchStatus.Text = $"Search failed: {ex.Message}";
        }
        finally
        {
            SearchProgress.IsActive = false;
        }
    }

    private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            SearchButton_Click(sender, new RoutedEventArgs());
        }
    }

    private void SearchResult_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is MetadataSearchResult result)
        {
            ViewModel.ApplySearchResult(result);
            SearchFlyout.Hide();
        }
    }

    private void AddVideo_Click(object sender, RoutedEventArgs e)
    {
        var url = NewVideoUrl.Text?.Trim();
        if (!string.IsNullOrEmpty(url))
        {
            ViewModel.Videos.Add(url);
            NewVideoUrl.Text = "";
        }
    }

    private void RemoveVideo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoUrl)
        {
            ViewModel.Videos.Remove(videoUrl);
        }
    }

    private void AddScreenshot_Click(object sender, RoutedEventArgs e)
    {
        var url = NewScreenshotUrl.Text?.Trim();
        if (!string.IsNullOrEmpty(url))
        {
            ViewModel.Screenshots.Add(url);
            NewScreenshotUrl.Text = "";
        }
    }

    private void RemoveScreenshot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string screenshotUrl)
        {
            ViewModel.Screenshots.Remove(screenshotUrl);
        }
    }
}

public partial class MetadataEditorViewModel : ObservableObject
{
    private readonly GameViewModel _originalGame;
    private readonly GrpcClientService _grpc;

    [ObservableProperty] private string title;
    [ObservableProperty] private string developer;
    [ObservableProperty] private string publisher;
    [ObservableProperty] private string description;
    [ObservableProperty] private string coverImageUrl;
    [ObservableProperty] private string backgroundImageUrl;
    [ObservableProperty] private string logoImageUrl;
    [ObservableProperty] private string steamId;
    [ObservableProperty] private string launchArguments;
    [ObservableProperty] private string genresText;
    [ObservableProperty] private double metacriticScore;

    public ObservableCollection<string> Videos { get; } = new();
    public ObservableCollection<string> Screenshots { get; } = new();

    [ObservableProperty] private ObservableCollection<MetadataSearchResult> searchResults = new();
    [ObservableProperty] private bool hasSearchResults;

    public MetadataEditorViewModel(GameViewModel game)
    {
        _originalGame = game;
        _grpc = (Application.Current as App)!.Services.GetRequiredService<GrpcClientService>();

        // Init fields
        Title = game.Title;
        Developer = game.Developer;
        Publisher = game.Publisher;
        Description = game.Description;
        CoverImageUrl = game.CoverImageUrl ?? "";
        BackgroundImageUrl = game.BackgroundImageUrl ?? "";
        LogoImageUrl = game.LogoImageUrl ?? "";
        SteamId = game.SteamId ?? "";
        LaunchArguments = game.LaunchArguments ?? "";
        MetacriticScore = game.MetacriticScore;
        GenresText = string.Join(", ", game.Genres);

        // Init videos and screenshots
        if (game.Videos != null)
        {
            foreach (var video in game.Videos)
            {
                Videos.Add(video);
            }
        }
        if (game.Screenshots != null)
        {
            foreach (var screenshot in game.Screenshots)
            {
                Screenshots.Add(screenshot);
            }
        }
    }

    public async Task SearchMetadataAsync(string query, string provider = "")
    {
        var request = new MetadataSearchRequest
        {
            Query = query,
            Provider = provider
        };

        var response = await _grpc.Client.SearchMetadataProvidersAsync(request);

        SearchResults.Clear();
        foreach (var result in response.Results)
        {
            SearchResults.Add(result);
        }
        HasSearchResults = SearchResults.Count > 0;
    }

    public void ApplySearchResult(MetadataSearchResult result)
    {
        // Apply ALL available metadata from the search result (matching macOS behavior)
        Title = result.Title;
        Developer = result.Developer;
        Publisher = result.Publisher;
        Description = result.Description;
        CoverImageUrl = result.CoverImageUrl;
        BackgroundImageUrl = result.BackgroundImageUrl;

        if (!string.IsNullOrEmpty(result.LogoImageUrl))
        {
            LogoImageUrl = result.LogoImageUrl;
        }

        if (result.Videos.Count > 0)
        {
            Videos.Clear();
            foreach (var video in result.Videos)
            {
                Videos.Add(video);
            }
        }

        if (result.Screenshots.Count > 0)
        {
            Screenshots.Clear();
            foreach (var screenshot in result.Screenshots)
            {
                Screenshots.Add(screenshot);
            }
        }

        if (result.Genres.Count > 0)
        {
            GenresText = string.Join(", ", result.Genres);
        }

        MetacriticScore = result.MetacriticScore;

        // Auto-set SteamId if the result comes from Steam provider
        if (result.Provider == "Steam" && !string.IsNullOrEmpty(result.ExternalId))
        {
            SteamId = result.ExternalId;
        }
    }

    public async Task SaveAsync()
    {
        var request = new GameMetadataUpdate
        {
            GameId = _originalGame.Id,
            Title = Title,
            Developer = Developer,
            Publisher = Publisher,
            Description = Description,
            CoverImageUrl = CoverImageUrl,
            BackgroundImageUrl = BackgroundImageUrl,
            LogoImageUrl = LogoImageUrl,
            SteamId = SteamId,
            LaunchArguments = LaunchArguments,
            MetacriticScore = (int)MetacriticScore
        };

        // Handle Genres
        var genres = GenresText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        request.Genres.AddRange(genres.Select(g => g.Trim()));

        // Handle Videos
        request.Videos.AddRange(Videos);

        // Handle Screenshots
        request.Screenshots.AddRange(Screenshots);

        await _grpc.Client.UpdateGameMetadataAsync(request);

        // Optimistically update the model
        _originalGame.Title = Title;
        _originalGame.Developer = Developer;
        _originalGame.Publisher = Publisher;
        _originalGame.Description = Description;
        _originalGame.CoverImageUrl = CoverImageUrl;
        _originalGame.BackgroundImageUrl = BackgroundImageUrl;
        _originalGame.LogoImageUrl = LogoImageUrl;
        _originalGame.SteamId = SteamId;
        _originalGame.LaunchArguments = LaunchArguments;
        _originalGame.MetacriticScore = MetacriticScore;
        _originalGame.Genres = genres.Select(g => g.Trim()).ToArray();
        _originalGame.Videos = Videos.ToArray();
        _originalGame.Screenshots = Screenshots.ToArray();
    }
}
