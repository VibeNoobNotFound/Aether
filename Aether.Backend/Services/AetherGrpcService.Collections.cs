using Aether.Backend.Data;
using Aether.Protos;
using Grpc.Core;

namespace Aether.Backend.Services;

/// <summary>
/// gRPC service implementation for Collections and Carousel management
/// </summary>
public partial class AetherGrpcService
{
    public override Task<CollectionList> GetCollections(Empty request, ServerCallContext context)
    {
        var collections = _database.GetAllCollections();
        var response = new CollectionList();

        foreach (var col in collections)
        {
            var gameCount = _database.GetGamesForCollection(col).Count();
            response.Collections.Add(MapToProto(col, gameCount));
        }

        return Task.FromResult(response);
    }

    public override Task<Collection> CreateCollection(CreateCollectionRequest request, ServerCallContext context)
    {
        var entity = new CollectionEntity
        {
            Name = request.Name,
            IconName = string.IsNullOrEmpty(request.IconName) ? "folder.fill" : request.IconName,
            Type = Data.CollectionType.Custom,
            IsSystem = false
        };

        var id = _database.CreateCollection(entity);
        entity.Id = id;

        return Task.FromResult(MapToProto(entity, 0));
    }

    public override Task<OperationStatus> UpdateCollection(UpdateCollectionRequest request, ServerCallContext context)
    {
        var col = _database.GetCollectionById(request.Id);
        if (col == null)
        {
            return Task.FromResult(new OperationStatus { Success = false, Message = "Collection not found" });
        }

        if (request.HasName) col.Name = request.Name;
        if (request.HasIconName) col.IconName = request.IconName;
        if (request.HasSortOrder) col.SortOrder = request.SortOrder;
        if (request.HasIsVisible) col.IsVisible = request.IsVisible;

        _database.UpdateCollection(col);
        return Task.FromResult(new OperationStatus { Success = true });
    }

    public override Task<OperationStatus> DeleteCollection(CollectionId request, ServerCallContext context)
    {
        var success = _database.DeleteCollection(request.Id);
        return Task.FromResult(new OperationStatus
        {
            Success = success,
            Message = success ? "" : "Cannot delete system collection"
        });
    }

    public override Task<OperationStatus> AddGameToCollection(CollectionGameAction request, ServerCallContext context)
    {
        if (!int.TryParse(request.GameId, out var gameId))
        {
            return Task.FromResult(new OperationStatus { Success = false, Message = "Invalid game ID" });
        }

        _database.AddGameToCollection(request.CollectionId, gameId);
        return Task.FromResult(new OperationStatus { Success = true });
    }

    public override Task<OperationStatus> RemoveGameFromCollection(CollectionGameAction request, ServerCallContext context)
    {
        if (!int.TryParse(request.GameId, out var gameId))
        {
            return Task.FromResult(new OperationStatus { Success = false, Message = "Invalid game ID" });
        }

        _database.RemoveGameFromCollection(request.CollectionId, gameId);
        return Task.FromResult(new OperationStatus { Success = true });
    }

    public override async Task GetCollectionGames(CollectionId request, IServerStreamWriter<Game> responseStream, ServerCallContext context)
    {
        var col = _database.GetCollectionById(request.Id);
        if (col == null) return;

        var games = _database.GetGamesForCollection(col);
        foreach (var game in games)
        {
            await responseStream.WriteAsync(MapToProto(game));
        }
    }

    public override Task<OperationStatus> ReorderCollections(ReorderCollectionsRequest request, ServerCallContext context)
    {
        _database.ReorderCollections(request.CollectionIds.ToList());
        return Task.FromResult(new OperationStatus { Success = true });
    }

    // Carousel
    public override Task<Aether.Protos.CarouselConfig> GetCarouselConfig(Empty request, ServerCallContext context)
    {
        var config = _database.GetCarouselConfig();
        var response = new Aether.Protos.CarouselConfig
        {
            MaxGames = config.MaxGames
        };

        if (config.CollectionId.HasValue)
        {
            response.CollectionId = config.CollectionId.Value;
        }

        foreach (var id in config.GameIds)
        {
            response.GameIds.Add(id.ToString());
        }

        return Task.FromResult(response);
    }

    public override Task<OperationStatus> SetCarouselConfig(Aether.Protos.CarouselConfig request, ServerCallContext context)
    {
        var config = new Data.CarouselConfig
        {
            CollectionId = request.HasCollectionId ? request.CollectionId : null,
            GameIds = request.GameIds.Select(s => int.TryParse(s, out var id) ? id : 0).Where(id => id > 0).ToList(),
            MaxGames = request.MaxGames > 0 ? request.MaxGames : 5
        };

        _database.SetCarouselConfig(config);
        return Task.FromResult(new OperationStatus { Success = true });
    }

    public override async Task GetCarouselGames(Empty request, IServerStreamWriter<Game> responseStream, ServerCallContext context)
    {
        var games = _database.GetCarouselGames();
        foreach (var game in games)
        {
            await responseStream.WriteAsync(MapToProto(game));
        }
    }

    // Mapping helper
    private static Collection MapToProto(CollectionEntity entity, int gameCount)
    {
        var proto = new Collection
        {
            Id = entity.Id,
            Name = entity.Name,
            IconName = entity.IconName,
            Type = (Aether.Protos.CollectionType)entity.Type,
            IsSystem = entity.IsSystem,
            SortOrder = entity.SortOrder,
            IsVisible = entity.IsVisible,
            GameCount = gameCount
        };

        if (!string.IsNullOrEmpty(entity.PlatformFilter))
        {
            proto.PlatformFilter = entity.PlatformFilter;
        }

        proto.GameIds.AddRange(entity.GameIds);
        return proto;
    }
}
