using global::Aether.Protos;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aether.WinUI.Models;

public partial class PluginViewModel : ObservableObject
{
    private static ILogger<PluginViewModel> Logger =>
        (Ioc.Default.GetService<ILogger<PluginViewModel>>()) ?? NullLogger<PluginViewModel>.Instance;
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string version = "";
    [ObservableProperty] private string author = "";
    [ObservableProperty] private string website = "";
    [ObservableProperty] private bool isEnabled;
    [ObservableProperty] private bool supportsManualAddition;
    [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<string> capabilities = new();

    public string AuthorBinding => $"by {Author}";

    public static PluginViewModel FromProto(PluginInfo proto)
    {
        Logger.LogDebug("PluginViewModel.FromProto: {PluginName}", proto.Name);
        var vm = new PluginViewModel
        {
            Name = proto.Name,
            Version = proto.Version,
            Author = proto.Author,
            IsEnabled = proto.IsEnabled,
            SupportsManualAddition = proto.SupportsManualAddition
        };

        if (proto.IsImporter) vm.Capabilities.Add("Importer");
        if (proto.IsMetadataProvider) vm.Capabilities.Add("Metadata");
        if (proto.IsGameLauncher) vm.Capabilities.Add("Launcher");
        if (proto.IsNewsProvider) vm.Capabilities.Add("News");

        return vm;
    }
}
