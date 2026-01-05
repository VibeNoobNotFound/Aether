using LiteDB;

namespace Aether.Backend.Data;

/// <summary>
/// Configuration for the Hero Carousel game source
/// </summary>
public class CarouselConfig
{
    [BsonId]
    public int Id { get; set; } = 1; // Singleton record
    
    /// <summary>
    /// If set, carousel shows games from this collection
    /// </summary>
    public int? CollectionId { get; set; }
    
    /// <summary>
    /// If CollectionId is null, use these specific game IDs
    /// If both are empty, falls back to first N games
    /// </summary>
    public List<int> GameIds { get; set; } = new();
    
    /// <summary>
    /// Maximum number of games to show in carousel
    /// </summary>
    public int MaxGames { get; set; } = 5;
    
    public DateTime UpdatedAt { get; set; }
}
