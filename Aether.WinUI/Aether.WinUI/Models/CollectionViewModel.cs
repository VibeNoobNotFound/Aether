using global::Aether.Protos;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;

namespace Aether.WinUI.Models;

public partial class CollectionViewModel : ObservableObject
{
    private static ILogger<CollectionViewModel> Logger =>
        (Ioc.Default.GetService<ILogger<CollectionViewModel>>()) ?? NullLogger<CollectionViewModel>.Instance;
    [ObservableProperty] private int id;
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string icon = ""; // Renamed from IconName to match app usage
    [ObservableProperty] private CollectionType type;
    [ObservableProperty] private bool isSystem;
    [ObservableProperty] private string? platformFilter;
    [ObservableProperty] private int sortOrder;
    [ObservableProperty] private bool isVisible;
    [ObservableProperty] private int gameCount;
    // WinUI app uses string IDs for games, but Proto uses int32 (database IDs likely).
    // We need to map this carefully. If Proto Game uses string ID, why does Collection use int32?
    // Checking Proto: Game.id is string. Collection.game_ids is int32. This is a mismatch in Proto definition?
    // "repeated int32 game_ids = 7;" -> If Game.Id is string (GUID?), this is wrong.
    // However, looking at Aether.Unix, maybe it uses int IDs?
    // Re-reading Proto: "message Game { string id = 1; ..."
    // Re-reading Proto: "repeated int32 game_ids = 7;" inside Collection.
    // This looks like a schema bug OR internal ID usage.
    // For parity, I will cast/convert if possible, or assume they are parsable integers?
    // Wait, "repeated string game_ids = 2;" in CarouselConfig.
    // Let's explicitly check how macOS handles it.
    // macOS: `collection.gameIds.map { String($0) }` implies they are Ints converting to Strings.

    [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<string> gameIds = new();

    public static CollectionViewModel FromProto(Collection proto)
    {
        Logger.LogDebug("CollectionViewModel.FromProto: {CollectionId}", proto.Id);
        var vm = new CollectionViewModel
        {
            Id = proto.Id,
            Name = proto.Name,
            Icon = proto.IconName, // Map IconName -> Icon
            Type = proto.Type,
            IsSystem = proto.IsSystem,
            PlatformFilter = proto.HasPlatformFilter ? proto.PlatformFilter : null,
            SortOrder = proto.SortOrder,
            IsVisible = proto.IsVisible,
            GameCount = proto.GameCount
        };

        foreach (var id in proto.GameIds)
        {
            vm.GameIds.Add(id.ToString());
        }

        return vm;
    }
}
