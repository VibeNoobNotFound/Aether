using global::Aether.Protos;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;

namespace Aether.WinUI.Models;

public partial class CollectionViewModel : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string iconName = "";
    [ObservableProperty] private CollectionType type;
    [ObservableProperty] private bool isSystem;
    [ObservableProperty] private int sortOrder;
    [ObservableProperty] private bool isVisible;
    [ObservableProperty] private int gameCount;
    [ObservableProperty] private int[] gameIds = System.Array.Empty<int>();

    public static CollectionViewModel FromProto(Collection proto)
    {
        return new CollectionViewModel
        {
            Id = proto.Id,
            Name = proto.Name,
            IconName = proto.IconName,
            Type = proto.Type,
            IsSystem = proto.IsSystem,
            SortOrder = proto.SortOrder,
            IsVisible = proto.IsVisible,
            GameCount = proto.GameCount,
            GameIds = proto.GameIds.ToArray()
        };
    }
}
