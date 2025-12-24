using Aether.PluginSDK.UI;

namespace Aether.PluginSDK;

public interface IPlugin
{
    string Name { get; }
    string Author { get; }
    string Version { get; }

    // Hook 1: Library
    Task OnLibraryScan(LibraryContext context);

    // Hook 2: UI (The Interactive Part)
    List<Widget> GetWidgets(Game game);

    // Hook 3: Action (returns result with optional games to add)
    Task<WidgetActionResult> OnWidgetAction(string actionId, string payload);

    // Hook 4: Lifecycle
    Task OnGameLaunched(Game game);
    Task OnGameStopped(Game game, TimeSpan sessionDuration);

    // Hook 5: Setup (Server-Driven UI for adding games)
    List<Widget> GetSetupWidgets();
}


public class LibraryContext
{
    // Placeholder
}

public interface INewsProvider
{
    Task<List<NewsItem>> GetNewsAsync(string gameId);
    Task<List<NewsItem>> GetGeneralNewsAsync();
}

public class NewsItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string ContentHtml { get; set; } = "";
    public string Author { get; set; } = "";
    public long DateUnix { get; set; }
    public string ImageUrl { get; set; } = "";
    public string Source { get; set; } = "";
}

public class Game
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    public string Platform { get; set; } = "";
    // Add other fields as needed
}
