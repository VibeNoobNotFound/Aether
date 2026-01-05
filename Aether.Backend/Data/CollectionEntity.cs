using LiteDB;

namespace Aether.Backend.Data;

/// <summary>
/// Type of collection - determines how games are resolved
/// </summary>
public enum CollectionType
{
    Custom = 0,         // User-created, manually populated with game IDs
    Favorites = 1,      // System: Games where IsFavorite == true
    RecentlyPlayed = 2, // System: Games ordered by LastPlayed desc
    Platform = 3        // System: Games filtered by Platform
}

/// <summary>
/// Represents a game collection (system or user-created)
/// </summary>
public class CollectionEntity
{
    [BsonId]
    public int Id { get; set; }
    
    public string Name { get; set; } = "";
    public string IconName { get; set; } = "folder.fill"; // SF Symbol
    public CollectionType Type { get; set; } = CollectionType.Custom;
    
    /// <summary>
    /// If true, this is a system collection that cannot be deleted
    /// </summary>
    public bool IsSystem { get; set; }
    
    /// <summary>
    /// For Platform type: the platform name to filter by (e.g., "Steam", "Epic")
    /// </summary>
    public string? PlatformFilter { get; set; }
    
    /// <summary>
    /// For Custom type: list of game database IDs manually added to this collection
    /// </summary>
    public List<int> GameIds { get; set; } = new();
    
    /// <summary>
    /// Display order on the homepage (lower = higher on page)
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>
    /// Whether to show this collection on the homepage
    /// </summary>
    public bool IsVisible { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
