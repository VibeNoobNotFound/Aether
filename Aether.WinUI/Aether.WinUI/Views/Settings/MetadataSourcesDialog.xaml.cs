using Aether.Protos;
using Aether.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Aether.WinUI.Views.Settings;

public sealed partial class MetadataSourcesDialog : ContentDialog
{
    private readonly GrpcClientService _grpc;
    public ObservableCollection<string> Providers { get; } = new();
    private readonly ILogger<MetadataSourcesDialog> _logger;

    public MetadataSourcesDialog()
    {
        InitializeComponent();
        _grpc = Ioc.Default.GetRequiredService<GrpcClientService>();
        _logger = Ioc.Default.GetRequiredService<ILogger<MetadataSourcesDialog>>();
        _logger.LogDebug("MetadataSourcesDialog initialized");
        Opened += MetadataSourcesDialog_Opened;
        PrimaryButtonClick += MetadataSourcesDialog_PrimaryButtonClick;
    }

    private async void MetadataSourcesDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        _logger.LogInformation("MetadataSourcesDialog opened");
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _logger.LogInformation("Loading metadata providers");
        Providers.Clear();
        var settings = await _grpc.Client.GetMetadataSettingsAsync(new Empty());

        var priority = settings.ProviderPriority.ToList();
        var available = settings.AvailableProviders.ToList();

        foreach (var provider in priority)
        {
            Providers.Add(provider);
        }

        foreach (var provider in available)
        {
            if (!Providers.Contains(provider))
            {
                Providers.Add(provider);
            }
        }
    }

    private async void MetadataSourcesDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        _logger.LogInformation("MetadataSourcesDialog primary button clicked");
        var deferral = args.GetDeferral();
        try
        {
            var request = new MetadataSettings();
            request.ProviderPriority.AddRange(Providers);
            await _grpc.Client.SetMetadataSettingsAsync(request);
            _logger.LogInformation("Metadata provider order saved");
        }
        finally
        {
            deferral.Complete();
        }
    }
}
