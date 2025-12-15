# 🧩 Building Plugins for Aether

Aether's power comes from its modular plugin system. This guide will walk you through creating your own Importer or Metadata Provider using .NET 10.

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
You need to reference the `Aether.PluginSDK`. If you are building inside the main repo, reference the project directly. If you are external, you would reference the `Aether.PluginSDK.dll`.

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Aether.PluginSDK\Aether.PluginSDK.csproj" />
</ItemGroup>
```

---

## 🏗 Core Interfaces

Every plugin must implement `IPlugin`. Optionally, you can implement `ILibraryImporter` or `IMetadataProvider`.

### `IPlugin` (Required)
The base contract for lifecycle management.
```csharp
public interface IPlugin
{
    string Name { get; }
    
    // Lifecycle Hooks
    Task OnLibraryScan(LibraryContext context);
    Task OnGameLaunched(Game game);
    Task OnGameStopped(Game game, TimeSpan sessionDuration);
    
    // UI Hooks
    List<Widget> GetSetupWidgets(); // Configuration UI
    Task OnWidgetAction(string actionId, string payload);
}
```

### `ILibraryImporter` (Optional)
Implement this if your plugin finds installed games (e.g., from a launcher or folder).

```csharp
public interface ILibraryImporter : IPlugin
{
    Task<bool> CanImportAsync();
    
    // Yields games one by one as they are found
    IAsyncEnumerable<ImportedGame> ScanLibraryAsync(IProgress<ScanProgress>? progress = null);
}
```

### `IMetadataProvider` (Optional)
Implement this if your plugin fetches game details/covers from an API.

```csharp
public interface IMetadataProvider
{
    Task<GameMetadata?> SearchAsync(string gameName, string? platform = null);
    Task<GameMetadata?> GetByIdAsync(string gameId);
    Task<List<string>> GetScreenshotsAsync(string gameId);
}
```

---

## 💻 Example: Simple File Importer

Here is a complete example of a plugin that imports `.nes` ROMs from a specific folder.

```csharp
using Aether.PluginSDK;
using Aether.PluginSDK.Library;

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
        int count = 0;

        foreach (var romPath in roms)
        {
            count++;
            var title = Path.GetFileNameWithoutExtension(romPath);
            
            // Report progress back to UI
            progress?.Report(new ScanProgress("NES", roms.Length, count, title, (double)count / roms.Length * 100));

            yield return new ImportedGame(
                Title: title,
                Platform: "Nintendo Entertainment System",
                Id: romPath, // Unique ID
                InstallPath: Path.GetDirectoryName(romPath),
                ExecutablePath: romPath // Launcher handles opening this
            );
        }
    }

    // Required IPlugin stubs
    public List<Widget> GetSetupWidgets() => new List<Widget>();
    public Task OnLibraryScan(LibraryContext context) => Task.CompletedTask;
    public Task OnWidgetAction(string actionId, string payload) => Task.CompletedTask;
    public Task OnGameLaunched(Game game) => Task.CompletedTask;
    public Task OnGameStopped(Game game, TimeSpan sessionDuration) => Task.CompletedTask;
}
```

---

## 🎨 Server-Driven UI

Plugins can expose configuration settings without writing any frontend code. The backend defines the UI layout in JSON, and the macOS frontend renders it natively.

Implement `GetSetupWidgets()` to return a `Form`:

```csharp
public List<Widget> GetSetupWidgets()
{
    return new List<Widget>
    {
        new Widget
        {
            Title = "Login to Service",
            LayoutJson = @"
            {
                ""type"": ""Form"",
                ""fields"": [
                    { ""id"": ""username"", ""type"": ""Text"", ""label"": ""Username"" },
                    { ""id"": ""password"", ""type"": ""SecureText"", ""label"": ""Password"" }
                ],
                ""actions"": [
                    { ""id"": ""login_btn"", ""label"": ""Log In"", ""actionType"": ""submit"" }
                ]
            }"
        }
    };
}
```

Handle the action in `OnWidgetAction`:

```csharp
public async Task OnWidgetAction(string actionId, string payload)
{
    if (actionId == "login_btn")
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(payload);
        var username = data["username"];
        // Authenticate...
    }
}
```

---

## 📦 Deployment

1.  Build your plugin (`dotnet build -c Release`).
2.  Drop the compiled `.dll` into the `plugins/` directory next to the `Aether.Backend` executable.
3.  Restart Aether.
