using Aether.Protos;
using Aether.WinUI.Models;
using Aether.WinUI.Services; // Ensure this is imported for BackendManager if exposed or use App.Services
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
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
            // TODO: parameters.Cancel = true; // Keep dialog open on error?
        }
        finally
        {
            deferral.Complete();
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

        await _grpc.Client.UpdateGameMetadataAsync(request);
        
        // Optimistically update the model or wait for refresh?
        // Ideally the backend sends an event or we refresh.
        // For now, let's update the specific object if it's shared, but GameViewModel is a copy from listing usually.
        // We'll rely on Refresh or Re-fetch.
        // But to be nice, we can update local props.
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
    }
}
