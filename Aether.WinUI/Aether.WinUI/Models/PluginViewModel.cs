using global::Aether.Protos;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aether.WinUI.Models;

public partial class PluginViewModel : ObservableObject
{
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string version = "";
    [ObservableProperty] private string author = "";
    [ObservableProperty] private string website = "";
    [ObservableProperty] private bool isEnabled;
    [ObservableProperty] private bool supportsManualAddition;

    public static PluginViewModel FromProto(PluginInfo proto)
    {
        return new PluginViewModel
        {
            Name = proto.Name,
            Version = proto.Version,
            Author = proto.Author,
            // Website = proto.Website, // Not in proto
            IsEnabled = true, // Assuming enabled by default or fetch from separate config
            SupportsManualAddition = proto.SupportsManualAddition
        };
    }
}
