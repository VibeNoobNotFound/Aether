# 🧩 Building Plugins for Aether

Aether's power comes from its modular plugin system. This guide will walk you through creating your own Importer or Metadata Provider using .NET.

## 🚀 Getting Started

### Prerequisites
*   **.NET 10 SDK** installed.
*   A C# Class Library project.

### 1. Create a Project
Create a new Class Library for your plugin.
```bash
dotnet new classlib -n Aether.Plugins.MyLauncher
cd Aether.Plugins.MyLauncher
```

### 2. Add Reference to SDK
You need to reference the `Aether.PluginSDK`.

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Aether.PluginSDK\Aether.PluginSDK.csproj" />
</ItemGroup>
```

---

## 🏗 Core Interfaces

Every plugin implements `IPlugin`. Depending on your goal, you might also implement `ILibraryImporter` or `IGameLauncher`.

### 1. The Five Lifecycle Hooks (`IPlugin`)
The `IPlugin` interface is the heart of your extension. It defines **5 Key Hooks**:

1.  **`OnLibraryScan(LibraryContext)`**: Called when the user clicks "Scan Library". Use this to find games on the disk.
2.  **`GetWidgets(Game)`**: Called when viewing a Game Detail page. Return `Widget` objects to render custom UI (e.g., "Install DLC" button).
3.  **`OnWidgetAction(ActionID, Payload)`**: Called when a user clicks a button in your widget.
4.  **`OnGameLaunched(Game)`**: Key lifecycle event for tracking playtime.
5.  **`OnGameStopped(Game, Duration)`**: Called when the game process exits. Use this to sync playtime with external services.
6.  **`GetSetupWidgets()`**: Returns UI for the Settings > Plugins page (e.g., Login forms).

### 2. Launching Games (`IGameLauncher`)
If your plugin handles a specific platform (like Steam or Epic), implement `IGameLauncher`.

```csharp
public interface IGameLauncher
{
    // Check if this plugin handles this specific game
    bool CanLaunch(LaunchContext context);

    // Execute the launch logic (Protocol URI, Executable, or Custom)
    Task<LaunchResult> LaunchAsync(LaunchContext context);
}
```

---

## 💻 Example: Simple File Importer

Here is a complete example of a plugin that imports `.nes` ROMs.

```csharp
using Aether.PluginSDK;

public class NesRomImporter : ILibraryImporter
{
    public string Name => "NES Importer";

    public async Task<bool> CanImportAsync()
    {
        return Directory.Exists("/Users/Shared/ROMs/NES");
    }

    public async IAsyncEnumerable<ImportedGame> ScanLibraryAsync(IProgress<ScanProgress>? progress = null)
    {
        var roms = Directory.GetFiles("/Users/Shared/ROMs/NES", "*.nes");
        foreach (var romPath in roms)
        {
            yield return new ImportedGame(
                Title: Path.GetFileNameWithoutExtension(romPath),
                Platform: "Nintendo Entertainment System",
                Id: romPath,
                InstallPath: Path.GetDirectoryName(romPath),
                ExecutablePath: romPath
            );
        }
    }
    
    // ... Implement other IPlugin stubs ...
}
```

---

## 🎨 Server-Driven UI (Widgets)

Plugins can expose configuration settings without writing any SwiftUI code. The backend defines the UI layout using the `WidgetBuilder` helper, and the macOS frontend renders it natively.

### Example: Login Form
Implement `GetPluginWidgets(WidgetLocation)` to return widgets based on context:

```csharp
public List<Widget> GetPluginWidgets(WidgetLocation location)
{
    if (location == WidgetLocation.Settings)
    {
        return new List<Widget>
        {
            WidgetBuilder.Form("login_form", "Log In", "login_action",
                WidgetBuilder.Header("Account Settings"),
                WidgetBuilder.TextInput("username", "Username", required: true),
                WidgetBuilder.TextInput("password", "Password", required: true, secure: true)
            )
        };
    }
    return new List<Widget>();
}
```

Handle the action in `OnWidgetAction`:

```csharp
public async Task<WidgetActionResult> OnWidgetAction(string actionId, string payloadJson)
{
    if (actionId == "login_action")
    {
        // Parse payload (contains field values)
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(payloadJson);
        var username = data["username"];
        var password = data["password"];

        // ... authenticate ...
        return WidgetActionResult.Success("Logged in successfully!");
    }
    return WidgetActionResult.Failure("Unknown action");
}
```

---

## 📦 Deployment

1.  Build your plugin (`dotnet build -c Release`).
2.  Drop the compiled `.dll` into the `plugins/` directory next to the `Aether.Backend` executable.
3.  Restart Aether (Frontend connects to Backend on port **55551**).
