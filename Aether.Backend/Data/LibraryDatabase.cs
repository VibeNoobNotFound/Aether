using LiteDB;
using Microsoft.Extensions.Logging;

namespace Aether.Backend.Data;

/// <summary>
/// LiteDB database manager for game library
/// </summary>
public class LibraryDatabase : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<GameEntity> _games;
    private readonly ILiteCollection<GameSessionLog> _sessions;
    private readonly ILogger<LibraryDatabase> _logger;

    public LibraryDatabase(string databasePath, ILogger<LibraryDatabase> logger)
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
        _sessions = _db.GetCollection<GameSessionLog>("game_sessions");

        // Sesssion Indexes
        _sessions.EnsureIndex(x => x.GameId);
        _sessions.EnsureIndex(x => x.StartTime);

        // Create indexes for fast queries
        _games.EnsureIndex(x => x.Platform);
        _games.EnsureIndex(x => x.ExternalId);
        _games.EnsureIndex(x => x.Title);
        _games.EnsureIndex(x => x.IsInstalled);
        _games.EnsureIndex(x => x.IsFavorite);

        _logger.LogInformation("Database initialized at {Path}", databasePath);
    }

    /// <summary>
    /// Get database path based on OS
    /// </summary>
    public static string GetDefaultDatabasePath(out string baseDir)
    {

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
            _logger.LogDebug("Updated game: {Title} ({Platform})", game.Title, game.Platform);
            return existing.Id;
        }
        else
        {
            // Insert new
            game.ImportedAt = DateTime.UtcNow;
            game.UpdatedAt = DateTime.UtcNow;

            var id = _games.Insert(game);
            _logger.LogDebug("Inserted game: {Title} ({Platform})", game.Title, game.Platform);
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

    public void UpdatePlayCount(int gameId)
    {
        var game = GetGameById(gameId);
        if (game != null)
        {
            game.PlayCount++;
            game.LastPlayed = DateTime.UtcNow; // Usually associated with a play
            game.UpdatedAt = DateTime.UtcNow;
            _games.Update(game);
        }
    }

    public void UpdateLastPlayed(int gameId, DateTime time)
    {
        var game = GetGameById(gameId);
        if (game != null)
        {
            game.LastPlayed = time;
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
    /// Factory reset: Drop all collections and vacuum
    /// </summary>
    public void FactoryReset()
    {
        _db.DropCollection("games");
        _db.DropCollection("collections");
        _db.DropCollection("carousel_config");
        _db.DropCollection("metadata_config");
        _db.Checkpoint();

        // Also delete all plugin storage files (*.db) in the same directory, except library.db
        try
        {
            GetDefaultDatabasePath(out var baseDir);
            if (Directory.Exists(baseDir))
            {
                var files = Directory.GetFiles(baseDir, "*.db");
                foreach (var file in files)
                {
                    if (Path.GetFileName(file).ToLower() != "library.db")
                    {
                        try
                        {
                            File.Delete(file);
                            _logger.LogInformation("Deleted plugin storage: {File}", Path.GetFileName(file));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to delete plugin storage: {File}", Path.GetFileName(file));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing plugin storage");
        }
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

    #region Collections

    private ILiteCollection<CollectionEntity> Collections => _db.GetCollection<CollectionEntity>("collections");
    private ILiteCollection<CarouselConfig> CarouselConfigs => _db.GetCollection<CarouselConfig>("carousel_config");

    /// <summary>
    /// Initialize default collections on first run
    /// </summary>
    /// <summary>
    /// Initialize default collections on first run
    /// </summary>
    public void SeedDefaultCollections(IEnumerable<Aether.PluginSDK.Library.ILibraryImporter> importers)
    {
        if (Collections.Count() > 0) return; // Already seeded

        _logger.LogInformation("Seeding default collections...");

        int order = 0;
        var defaults = new List<CollectionEntity>
        {
            new()
            {
                Name = "Favorites", IconName = "heart.fill", Type = CollectionType.Favorites, IsSystem = true,
                SortOrder = order++
            },
            new()
            {
                Name = "Recently Played", IconName = "clock.fill", Type = CollectionType.RecentlyPlayed,
                IsSystem = true, SortOrder = order++
            },
            // Platform collections generated from plugins
        };

        foreach (var importer in importers)
        {
            // Use a mapping or heuristic for icons if possible, otherwise default to gamecontroller
            var icon = "gamecontroller.fill";
            if (importer.Name.Contains("Apple", StringComparison.OrdinalIgnoreCase)) icon = "apple.logo";
            if (importer.Name.Contains("Web", StringComparison.OrdinalIgnoreCase)) icon = "globe";
            if (importer.Name.Contains("CrossOver", StringComparison.OrdinalIgnoreCase)) icon = "desktopcomputer";

            defaults.Add(new CollectionEntity
            {
                Name = importer.Name,
                IconName = icon,
                Type = CollectionType.Platform,
                IsSystem = true,
                PlatformFilter = importer.Name,
                SortOrder = order++
            });
        }

        // Always add Custom Games fallback if desired, or skip it. User requested plugins only.
        // Let's keep "Custom Games" manual collection if it's not a platform import?
        // Actually user said "plugins names of the ones that have ILibraryImporter".
        // So we strictly stick to that list plus Favorites/Recent.

        foreach (var col in defaults)
        {
            col.CreatedAt = DateTime.UtcNow;
            col.UpdatedAt = DateTime.UtcNow;
            Collections.Insert(col);
        }

        _logger.LogInformation("Seeded {Count} default collections", defaults.Count);
    }

    public IEnumerable<CollectionEntity> GetAllCollections()
    {
        return Collections.FindAll().OrderBy(c => c.SortOrder);
    }

    public CollectionEntity? GetCollectionById(int id)
    {
        return Collections.FindById(id);
    }

    public int CreateCollection(CollectionEntity collection)
    {
        collection.CreatedAt = DateTime.UtcNow;
        collection.UpdatedAt = DateTime.UtcNow;
        // Set sort order to end
        collection.SortOrder = Collections.Count();
        return Collections.Insert(collection);
    }

    public bool UpdateCollection(CollectionEntity collection)
    {
        collection.UpdatedAt = DateTime.UtcNow;
        return Collections.Update(collection);
    }

    public bool DeleteCollection(int id)
    {
        var col = GetCollectionById(id);
        if (col == null || col.IsSystem) return false; // Cannot delete system collections
        return Collections.Delete(id);
    }

    public void AddGameToCollection(int collectionId, int gameId)
    {
        var col = GetCollectionById(collectionId);
        if (col == null || col.Type != CollectionType.Custom) return;

        if (!col.GameIds.Contains(gameId))
        {
            col.GameIds.Add(gameId);
            col.UpdatedAt = DateTime.UtcNow;
            Collections.Update(col);
        }
    }

    public void RemoveGameFromCollection(int collectionId, int gameId)
    {
        var col = GetCollectionById(collectionId);
        if (col == null || col.Type != CollectionType.Custom) return;

        if (col.GameIds.Remove(gameId))
        {
            col.UpdatedAt = DateTime.UtcNow;
            Collections.Update(col);
        }
    }

    public void ReorderCollections(IList<int> orderedIds)
    {
        for (int i = 0; i < orderedIds.Count; i++)
        {
            var col = GetCollectionById(orderedIds[i]);
            if (col != null)
            {
                col.SortOrder = i;
                col.UpdatedAt = DateTime.UtcNow;
                Collections.Update(col);
            }
        }
    }

    /// <summary>
    /// Get games for a collection based on its type
    /// </summary>
    public IEnumerable<GameEntity> GetGamesForCollection(CollectionEntity collection)
    {
        return collection.Type switch
        {
            CollectionType.Favorites => _games.Find(g => g.IsFavorite),
            CollectionType.RecentlyPlayed => _games.FindAll()
                .Where(g => g.LastPlayed.HasValue)
                .OrderByDescending(g => g.LastPlayed),
            CollectionType.Platform => _games.Find(g => g.Platform == collection.PlatformFilter),
            CollectionType.Custom => collection.GameIds
                .Select(id => _games.FindById(id))
                .Where(g => g != null)!,
            _ => Enumerable.Empty<GameEntity>()
        };
    }

    #endregion

    #region Carousel Config

    public CarouselConfig GetCarouselConfig()
    {
        var config = CarouselConfigs.FindById(1);
        return config ?? new CarouselConfig();
    }

    public void SetCarouselConfig(CarouselConfig config)
    {
        config.Id = 1; // Ensure singleton
        config.UpdatedAt = DateTime.UtcNow;
        CarouselConfigs.Upsert(config);
    }

    /// <summary>
    /// Get games to display in carousel based on config
    /// </summary>
    public IEnumerable<GameEntity> GetCarouselGames()
    {
        var config = GetCarouselConfig();

        IEnumerable<GameEntity> games;

        if (config.CollectionId.HasValue)
        {
            var col = GetCollectionById(config.CollectionId.Value);
            games = col != null ? GetGamesForCollection(col) : _games.FindAll();
        }
        else if (config.GameIds.Count > 0)
        {
            games = config.GameIds
                .Select(id => _games.FindById(id))
                .Where(g => g != null)!;
        }
        else
        {
            // Default: mix of favorites and recent
            var favorites = _games.Find(g => g.IsFavorite).ToList();
            var recent = _games.FindAll()
                .OrderByDescending(g => g.LastPlayed ?? DateTime.MinValue)
                .Take(config.MaxGames)
                .ToList();

            games = favorites.Union(recent).DistinctBy(g => g.Id);
        }

        return games.Take(config.MaxGames);
    }

    #endregion

    #region Metadata Config

    private ILiteCollection<MetadataConfig> MetadataConfigs => _db.GetCollection<MetadataConfig>("metadata_config");

    public MetadataConfig GetMetadataConfig()
    {
        var config = MetadataConfigs.FindById(1);
        return config ?? new MetadataConfig();
    }

    public void SetMetadataConfig(MetadataConfig config)
    {
        config.Id = 1;
        config.UpdatedAt = DateTime.UtcNow;
        MetadataConfigs.Upsert(config);
    }

    #endregion

    #region Search

    public enum SortOption
    {
        RELEVANCE = 0,
        NAME = 1,
        RELEASE_DATE = 2,
        PLAYTIME = 3
    }

    public (List<GameEntity> Games, int TotalMatches) SearchLibrary(
        string query,
        List<string> platformFilter,
        List<string> genreFilter,
        SortOption sortBy,
        bool sortAscending,
        int limit)
    {
        var games = _games.FindAll();

        // 1. Filtering
        if (platformFilter != null && platformFilter.Count > 0)
            games = games.Where(g => platformFilter.Contains(g.Platform));

        if (genreFilter != null && genreFilter.Count > 0)
            games = games.Where(g => g.Genres != null && g.Genres.Any(genre => genreFilter.Contains(genre)));

        var filteredList = games.ToList();

        // 2. Scoring & Sorting
        if (string.IsNullOrWhiteSpace(query))
        {
            // No query: Just sort by requested field
            filteredList = SortGames(filteredList, sortBy, sortAscending).ToList();
        }
        else
        {
            // Search Query: Calculate scores
            var queryTokens = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var scoredGames = filteredList.Select(g =>
            {
                int score = CalculateSearchScore(g, query, queryTokens);
                return (Game: g, Score: score);
            })
            .Where(x => x.Score > 0);

            // Sort logic
            if (sortBy == SortOption.RELEVANCE)
            {
                // Primary: Score (Desc), Secondary: Name (Asc)
                scoredGames = scoredGames.OrderByDescending(x => x.Score).ThenBy(x => x.Game.Title);
            }
            else
            {
                // Primary: User selection, Secondary: Score
                var sorted = SortGames(scoredGames.Select(x => x.Game), sortBy, sortAscending);
                scoredGames = sorted.Select(g => (g, CalculateSearchScore(g, query, queryTokens)));
            }

            filteredList = scoredGames.Select(x => x.Game).ToList();
        }

        int total = filteredList.Count;
        var result = filteredList.Take(limit).ToList();

        return (result, total);
    }

    private int CalculateSearchScore(GameEntity game, string fullQuery, string[] tokens)
    {
        if (string.IsNullOrEmpty(game.Title)) return 0;

        int score = 0;
        string title = game.Title.ToLowerInvariant();
        string fullQueryLower = fullQuery.ToLowerInvariant();

        // 1. Exact Match (Highest Priority)
        if (title == fullQueryLower) return 100;

        // 2. Starts With (High Priority)
        if (title.StartsWith(fullQueryLower)) score += 80;

        // 3. Contains Full Query Substring (Medium Priority)
        else if (title.Contains(fullQueryLower)) score += 60;

        // 4. Token Matching
        int tokensMatched = 0;
        foreach (var token in tokens)
        {
            if (title.Contains(token))
            {
                score += 20; // Base score for token match

                // Bonus: Token starts a word
                if (title.StartsWith(token) || title.Contains(" " + token))
                    score += 10;

                tokensMatched++;
            }
        }

        // Penalty for extra unmatched words in title (Results closer to query length are better)
        // Only apply if we have matches
        if (score > 0)
        {
            int titleLengthPenalty = Math.Max(0, title.Length - fullQueryLower.Length);
            score -= Math.Min(10, titleLengthPenalty / 5); // Cap penalty
        }

        return score;
    }

    private IEnumerable<GameEntity> SortGames(IEnumerable<GameEntity> games, SortOption sortBy, bool ascending)
    {
        return sortBy switch
        {
            SortOption.NAME => ascending ? games.OrderBy(g => g.Title) : games.OrderByDescending(g => g.Title),
            SortOption.RELEASE_DATE => ascending ? games.OrderBy(g => g.ReleaseDate) : games.OrderByDescending(g => g.ReleaseDate),
            SortOption.PLAYTIME => ascending ? games.OrderBy(g => g.TotalPlaytime) : games.OrderByDescending(g => g.TotalPlaytime),
            _ => games // Default (or RELEVANCE fallback)
        };
    }

    #endregion

    #region Session Logging & Stats

    /// <summary>
    /// Log the start of a game session
    /// </summary>
    public int LogSessionStart(int gameId, DateTime startTime)
    {
        var log = new GameSessionLog
        {
            GameId = gameId,
            StartTime = startTime
        };
        return _sessions.Insert(log);
    }

    /// <summary>
    /// Log the end of a game session and update duration
    /// </summary>
    public void LogSessionEnd(int sessionId, DateTime endTime)
    {
        var log = _sessions.FindById(sessionId);
        if (log != null)
        {
            log.EndTime = endTime;
            log.Duration = endTime - log.StartTime;
            _sessions.Update(log);
        }
    }

    /// <summary>
    /// Get aggregated stats for the entire library
    /// </summary>
    public Aether.Protos.LibraryStatsResponse GetLibraryStats()
    {
        var response = new Aether.Protos.LibraryStatsResponse();

        // 1. Total Games
        response.TotalGames = _games.Count();

        // 2. Playtime & Sessions (from session logs)
        // If no sessions logged yet, fallback to GameEntity stats? 
        // For accurate new system, we rely on logs. But for imported history we might need GameEntity data.
        // Let's rely on new logs for "Active Days" but maybe combining legacy for total time is safer?
        // User requested "accurate" so let's use logs primarily, but if logs are empty, use specific GameEntity fields?
        // Actually, user said current system doesn't log exact times. So existing TotalPlaytime in GameEntity is just an aggregate sum.
        // We will return aggregate from GameEntity for TOTAL time to keep legacy data, but logs for session counts/days.

        // REVISED: Let's calculate from GameEntity for totals (since existing data is there) 
        // and use SessionLogs for specifics like ActiveDayCount which cannot differ.

        var games = _games.FindAll();
        long legacyTotalSeconds = 0;
        int legacyTotalSessions = 0;
        foreach (var g in games)
        {
            legacyTotalSeconds += (long)(g.TotalPlaytime?.TotalSeconds ?? 0);
            legacyTotalSessions += g.PlayCount;
        }

        response.TotalPlaytimeSeconds = legacyTotalSeconds;
        response.TotalSessions = legacyTotalSessions;

        // 3. Active Days (Unique days in session logs)
        // This MUST come from logs. Existing system didn't track days.
        var sessionDates = _sessions.Query()
            .Select(x => x.StartTime)
            .ToEnumerable()
            .Select(d => d.Date)
            .Distinct()
            .Count();

        // If usage is 0 but we have play counts, active days is at least 1? 
        // No, show strict log data.
        response.ActiveDayCount = sessionDates;

        // 4. Top Genres
        var genreCounts = new Dictionary<string, int>();
        foreach (var g in games)
        {
            if (g.Genres != null)
            {
                foreach (var genre in g.Genres)
                {
                    if (!genreCounts.ContainsKey(genre)) genreCounts[genre] = 0;
                    genreCounts[genre]++;
                }
            }
        }

        response.TopGenres.AddRange(
            genreCounts.OrderByDescending(x => x.Value)
            .Take(5)
            .Select(x => new Aether.Protos.GenreStat { Genre = x.Key, Count = x.Value })
        );

        return response;
    }

    #endregion

}


