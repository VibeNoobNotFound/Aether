using Aether.PluginSDK.UI;

namespace Aether.PluginSDK;

public interface IPlugin
{
    string Name { get; }
    
    // Hook 1: Library
    Task OnLibraryScan(LibraryContext context); 

    // Hook 2: UI (The Interactive Part)
    List<Widget> GetWidgets(Game game); 

    // Hook 3: Action
    Task OnWidgetAction(string actionId, string payload);
    
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

public class Game 
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    public string Platform { get; set; } = ""; 
    // Add other fields as needed
}
