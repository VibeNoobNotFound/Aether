using LiteDB;

namespace Aether.Backend.Data;

/// <summary>
/// Log of a single game session start and end time.
/// </summary>
public class GameSessionLog
{
    [BsonId]
    public int Id { get; set; }
    
    public int GameId { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
    
    // Calculated Duration (or stored if needed)
    public TimeSpan? Duration { get; set; }
}
