using Aether.PluginSDK.Library;

namespace Aether.PluginSDK.UI;

/// <summary>
/// Result returned from OnWidgetAction to allow plugins to request backend operations
/// </summary>
public class WidgetActionResult
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    
    /// <summary>
    /// Games to add/update in the library database
    /// When this is populated, the backend will upsert these games to the DB immediately
    /// </summary>
    public List<ImportedGame>? GamesToAdd { get; set; }
    
    /// <summary>
    /// Optional metadata to apply to the added games
    /// Key = ExternalId of the game, Value = Metadata to apply
    /// </summary>
    public Dictionary<string, GameMetadata>? GameMetadata { get; set; }
    
    /// <summary>
    /// Create a simple success result with no side effects
    /// </summary>
    public static WidgetActionResult Ok(string? message = null) => new() { Success = true, Message = message };
    
    /// <summary>
    /// Create a failure result
    /// </summary>
    public static WidgetActionResult Fail(string message) => new() { Success = false, Message = message };
    
    /// <summary>
    /// Create a result that adds games to the library
    /// </summary>
    public static WidgetActionResult AddGames(List<ImportedGame> games, Dictionary<string, GameMetadata>? metadata = null) 
        => new() { Success = true, GamesToAdd = games, GameMetadata = metadata };
}
