using LiteDB;
using Serilog;

namespace Aether.Backend.Data;

/// <summary>
/// LiteDB database manager for game library
/// </summary>
public class LibraryDatabase : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<GameEntity> _games;
    private readonly ILogger _logger;

    public LibraryDatabase(string databasePath, ILogger logger)
    {
        _logger = logger;

        // Ensure directory exists
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _db = new LiteDatabase(databasePath);
        _games = _db.GetCollection<GameEntity>("games");

        // Create indexes for fast queries
        _games.EnsureIndex(x => x.Platform);
        _games.EnsureIndex(x => x.ExternalId);
        _games.EnsureIndex(x => x.Title);
        _games.EnsureIndex(x => x.IsInstalled);
        _games.EnsureIndex(x => x.IsFavorite);

        _logger.Information("Database initialized at {Path}", databasePath);
    }

    /// <summary>
    /// Get database path based on OS
    /// </summary>
    public static string GetDefaultDatabasePath()
    {
        string baseDir;

        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            baseDir = Path.Combine(home, "Library", "Application Support", "Aether");
        }
        else if (OperatingSystem.IsWindows())
        {
            baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Aether");
        }
        else // Linux
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            baseDir = Path.Combine(home, ".local", "share", "Aether");
        }

        return Path.Combine(baseDir, "library.db");
    }

    /// <summary>
    /// Upsert a game (insert or update based on Platform + ExternalId)
    /// </summary>
    public int UpsertGame(GameEntity game)
    {
        var existing = _games.FindOne(x =>
            x.Platform == game.Platform &&
            x.ExternalId == game.ExternalId);

        if (existing != null)
        {
            // Update existing
            game.Id = existing.Id;
            game.ImportedAt = existing.ImportedAt; // Preserve original import time
            game.UpdatedAt = DateTime.UtcNow;

            // Merge stats
            if (!game.LastPlayed.HasValue) 
                game.LastPlayed = existing.LastPlayed;
            else if (existing.LastPlayed.HasValue && existing.LastPlayed > game.LastPlayed)
                game.LastPlayed = existing.LastPlayed; // Keep most recent

            if (!game.TotalPlaytime.HasValue)
                game.TotalPlaytime = existing.TotalPlaytime;
            else if (existing.TotalPlaytime.HasValue && existing.TotalPlaytime > game.TotalPlaytime)
                game.TotalPlaytime = existing.TotalPlaytime; // Keep highest playtime

            // Merge Favorites (don't overwrite favorite status on re-scan unless explicitly changed? Scan doesn't set Favorite usually)
            // GameEntity.FromImportedGame copies IsFavorite from metadata if available, but usually it's user-set locally.
            // Let's assume user local favorite status overrides import unless we have a reason.
            // Actually, FromImportedGame sets IsFavorite = entity.IsFavorite (Wait, ScanService sets it from where?)
            // ScanService sets IsFavorite = entity.IsFavorite (which is wrong, it should be false for new games).
            // Let's ensure we preserve local IsFavorite.
            game.IsFavorite = existing.IsFavorite; 

            _games.Update(game);
            _logger.Debug("Updated game: {Title} ({Platform})", game.Title, game.Platform);
            return existing.Id;
        }
        else
        {
            // Insert new
            game.ImportedAt = DateTime.UtcNow;
            game.UpdatedAt = DateTime.UtcNow;

            var id = _games.Insert(game);
            _logger.Debug("Inserted game: {Title} ({Platform})", game.Title, game.Platform);
            return id;
        }
    }

    /// <summary>
    /// Get all games
    /// </summary>
    public IEnumerable<GameEntity> GetAllGames()
    {
        return _games.FindAll();
    }

    /// <summary>
    /// Get game by ID
    /// </summary>
    public GameEntity? GetGameById(int id)
    {
        return _games.FindById(id);
    }

    /// <summary>
    /// Get games by platform
    /// </summary>
    public IEnumerable<GameEntity> GetGamesByPlatform(string platform)
    {
        return _games.Find(x => x.Platform == platform);
    }

    /// <summary>
    /// Get installed games
    /// </summary>
    public IEnumerable<GameEntity> GetInstalledGames()
    {
        return _games.Find(x => x.IsInstalled);
    }

    /// <summary>
    /// Get favorite games
    /// </summary>
    public IEnumerable<GameEntity> GetFavoriteGames()
    {
        return _games.Find(x => x.IsFavorite);
    }

    /// <summary>
    /// Search games by title
    /// </summary>
    public IEnumerable<GameEntity> SearchByTitle(string searchTerm)
    {
        return _games.Find(x => x.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Update playtime
    /// </summary>
    public void UpdatePlaytime(int gameId, TimeSpan sessionDuration)
    {
        var game = GetGameById(gameId);
        if (game != null)
        {
            game.TotalPlaytime = (game.TotalPlaytime ?? TimeSpan.Zero) + sessionDuration;
            game.LastPlayed = DateTime.UtcNow;
            game.UpdatedAt = DateTime.UtcNow;
            _games.Update(game);
        }
    }

    /// <summary>
    /// Toggle favorite status
    /// </summary>
    public void ToggleFavorite(int gameId)
    {
        var game = GetGameById(gameId);
        if (game != null)
        {
            game.IsFavorite = !game.IsFavorite;
            game.UpdatedAt = DateTime.UtcNow;
            _games.Update(game);
        }
    }

    /// <summary>
    /// Delete game by ID
    /// </summary>
    public bool DeleteGame(string id)
    {
        // Try parsing int ID
        if (int.TryParse(id, out int dbId))
        {
            return _games.Delete(dbId);
        }
        return false;
    }

    /// <summary>
    /// Clear all games from database
    /// </summary>
    public int ClearAllGames()
    {
        return _games.DeleteAll();
    }

    /// <summary>
    /// Get total game count
    /// </summary>
    public int GetGameCount()
    {
        return _games.Count();
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
