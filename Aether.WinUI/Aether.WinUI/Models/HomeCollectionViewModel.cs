using System.Collections.ObjectModel;

namespace Aether.WinUI.Models;

public sealed class HomeCollectionViewModel
{
    public CollectionViewModel Collection { get; }
    public ObservableCollection<GameViewModel> Games { get; }

    public HomeCollectionViewModel(CollectionViewModel collection, ObservableCollection<GameViewModel> games)
    {
        Collection = collection;
        Games = games;
    }
}
