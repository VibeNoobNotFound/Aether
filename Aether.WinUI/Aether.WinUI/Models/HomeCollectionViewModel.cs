using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.ObjectModel;

namespace Aether.WinUI.Models;

public sealed class HomeCollectionViewModel
{
    private static ILogger<HomeCollectionViewModel> Logger =>
        (Ioc.Default.GetService<ILogger<HomeCollectionViewModel>>()) ?? NullLogger<HomeCollectionViewModel>.Instance;
    public CollectionViewModel Collection { get; }
    public ObservableCollection<GameViewModel> Games { get; }

    public HomeCollectionViewModel(CollectionViewModel collection, ObservableCollection<GameViewModel> games)
    {
        Collection = collection;
        Games = games;
        Logger.LogDebug("HomeCollectionViewModel created: {CollectionId}", collection.Id);
    }
}
