using Aether.Protos;
using Aether.PluginSDK;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Aether.Backend.Plugins;
using Aether.Backend.Data;

namespace Aether.Backend.Services;

public partial class AetherGrpcService
{
    public override Task<PluginList> GetPlugins(Empty request, ServerCallContext context)
    {
        var response = new PluginList();

        // Add Library Importers
        foreach (var importer in _pluginManager.GetLibraryImporters())
        {
            var info = new PluginInfo
            {
                Name = importer.Name,
                Version = importer.Version,
                Author = importer.Author,
                IsImporter = true,
                IsMetadataProvider = false,
                SupportsManualAddition = importer.SupportsManualAddition
            };

            if (importer.SupportedPlatforms != null)
            {
                foreach (var p in importer.SupportedPlatforms)
                {
                    info.SupportedPlatforms.Add(p);
                }
            }

            response.Plugins.Add(info);
        }

        // Add pure plugins if any (that aren't importers)
        foreach (var plugin in _pluginManager.GetPlugins())
        {
            // Simple de-duplication based on name
            var exists = response.Plugins.Any(p => p.Name == plugin.Name);
            if (!exists)
            {
                var info = new PluginInfo
                {
                    Name = plugin.Name,
                    Version = plugin.Version,
                    Author = plugin.Author,
                    IsImporter = false,
                    IsMetadataProvider = false,
                    SupportsManualAddition = false // Pure plugins (like metadata providers) generally don't support this
                };

                if (plugin.SupportedPlatforms != null)
                {
                    foreach (var p in plugin.SupportedPlatforms)
                    {
                        info.SupportedPlatforms.Add(p);
                    }
                }

                response.Plugins.Add(info);
            }
        }

        return Task.FromResult(response);
    }

    public override Task<WidgetList> GetWidgets(WidgetRequest request, ServerCallContext context)
    {
        var response = new WidgetList();

        // Helper local function to add mapped widgets
        void AddWidgets(IEnumerable<Aether.PluginSDK.UI.Widget> sourceWidgets)
        {
            foreach (var w in sourceWidgets)
            {
                var mapped = MapWidget(w);
                if (mapped != null)
                {
                    response.Widgets.Add(mapped);
                }
            }
        }

        var location = (Aether.PluginSDK.UI.WidgetLocation)request.Location;

        var importer = _pluginManager.GetLibraryImporters().FirstOrDefault(p => p.Name == request.PluginName);
        if (importer != null)
        {
            AddWidgets(importer.GetPluginWidgets(location));
            return Task.FromResult(response);
        }

        var plugin = _pluginManager.GetPlugins().FirstOrDefault(p => p.Name == request.PluginName);
        if (plugin != null)
        {
            AddWidgets(plugin.GetPluginWidgets(location));
            return Task.FromResult(response);
        }

        return Task.FromResult(response);
    }

    public override Task<WidgetList> GetGameDetailWidgets(GameId request, ServerCallContext context)
    {
        // TODO: Implement game detail widgets from plugins
        return Task.FromResult(new WidgetList());
    }

    public override async Task<OperationStatus> TriggerPluginAction(PluginAction request, ServerCallContext context)
    {
        try
        {
            // Resolve plugin
            IPlugin? plugin = _pluginManager.GetPlugins().FirstOrDefault(p => p.Name == request.PluginName);
            if (plugin == null)
            {
                // Try importers
                plugin = _pluginManager.GetLibraryImporters().FirstOrDefault(i => i.Name == request.PluginName);
            }

            if (plugin == null)
            {
                return new OperationStatus { Success = false, Message = $"Plugin '{request.PluginName}' not found." };
            }

            var result = await plugin.OnWidgetAction(request.ActionId, request.PayloadJson);

            // Handle games returned by the plugin action
            if (result.GamesToAdd != null && result.GamesToAdd.Count > 0)
            {
                foreach (var importedGame in result.GamesToAdd)
                {
                    // Apply optional metadata if provided
                    PluginSDK.Library.GameMetadata? metadata = null;
                    if (result.GameMetadata != null && result.GameMetadata.TryGetValue(importedGame.ExternalId, out var meta))
                    {
                        metadata = meta;
                    }

                    var entity = GameEntity.FromImportedGame(importedGame, metadata);
                    _database.UpsertGame(entity);
                    _logger.LogInformation("Added custom game '{Title}' to library via plugin action", entity.Title);
                }
            }

            return new OperationStatus { Success = result.Success, Message = result.Message ?? "Action executed." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing plugin action");
            return new OperationStatus { Success = false, Message = ex.Message };
        }
    }

    public override async Task<OperationStatus> InstallPlugin(PluginFile request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Filename) || request.Data.IsEmpty)
            {
                return new OperationStatus { Success = false, Message = "Invalid plugin file." };
            }

            // Ensure filename is safe and has .dll extension
            var filename = Path.GetFileName(request.Filename);
            if (!filename.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return new OperationStatus { Success = false, Message = "Only .dll files are allowed." };
            }

            // Use the configured plugins directory
            var pluginsDir = AppDomain.CurrentDomain.BaseDirectory + "plugins";
            // Check for processing environment variable override (from bundling)
            var envPluginsPath = Environment.GetEnvironmentVariable("PLUGINS_PATH");
            if (!string.IsNullOrEmpty(envPluginsPath))
            {
                pluginsDir = envPluginsPath;
            }

            Directory.CreateDirectory(pluginsDir);
            var filePath = Path.Combine(pluginsDir, filename);

            await File.WriteAllBytesAsync(filePath, request.Data.ToByteArray());
            _logger.LogInformation("Installed plugin: {Filename}", filename);

            // Reload plugins
            _pluginManager.LoadPlugins();

            return new OperationStatus { Success = true, Message = "Plugin installed successfully." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing plugin");
            return new OperationStatus { Success = false, Message = ex.Message };
        }
    }

    public override Task<OperationStatus> UninstallPlugin(PluginName request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                return Task.FromResult(new OperationStatus { Success = false, Message = "Invalid plugin name." });
            }

            var plugin = _pluginManager.GetPlugins().FirstOrDefault(p => p.Name == request.Name);
            if (plugin != null)
            {
                // Fallback implementation: Try to delete [Name].dll
                var pluginsDir = AppDomain.CurrentDomain.BaseDirectory + "plugins";
                var envPluginsPath = Environment.GetEnvironmentVariable("PLUGINS_PATH");
                if (!string.IsNullOrEmpty(envPluginsPath))
                {
                    pluginsDir = envPluginsPath;
                }

                var predictedPath = Path.Combine(pluginsDir, $"{request.Name}.dll");

                if (File.Exists(predictedPath))
                {
                    File.Delete(predictedPath);
                    _logger.LogInformation("Uninstalled plugin file: {Path}", predictedPath);

                    // Reload plugins to reflect removal
                    _pluginManager.LoadPlugins();

                    return Task.FromResult(new OperationStatus { Success = true, Message = "Plugin uninstalled." });
                }

                return Task.FromResult(new OperationStatus { Success = false, Message = "Plugin file not found (naming mismatch perhaps?)" });
            }

            return Task.FromResult(new OperationStatus { Success = false, Message = "Plugin not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uninstalling plugin");
            return Task.FromResult(new OperationStatus { Success = false, Message = ex.Message });
        }
    }

    private static UIWidget? MapWidget(Aether.PluginSDK.UI.Widget widget)
    {
        var protoWidget = new UIWidget
        {
            Id = widget.Id,
            SortOrder = widget.SortOrder,
            Style = new WidgetStyle
            {
                BackgroundColor = widget.Style.BackgroundColor ?? "",
                PaddingHorizontal = widget.Style.PaddingHorizontal ?? 0,
                PaddingVertical = widget.Style.PaddingVertical ?? 0
            }
        };

        switch (widget.Content)
        {
            case Aether.PluginSDK.UI.TextContent text:
                protoWidget.Text = new TextWidget
                {
                    Text = text.Text,
                    Color = text.Color ?? "",
                    Variant = (TextWidget.Types.Variant)text.Variant
                };
                break;

            case Aether.PluginSDK.UI.ButtonContent btn:
                protoWidget.Button = new ButtonWidget
                {
                    Label = btn.Label,
                    Icon = btn.Icon ?? "",
                    ActionId = btn.ActionId,
                    PayloadJson = btn.PayloadJson ?? "",
                    Style = (ButtonWidget.Types.Style)btn.Style
                };
                break;

            case Aether.PluginSDK.UI.TextInputContent input:
                protoWidget.TextInput = new TextInputWidget
                {
                    Label = input.Label,
                    Placeholder = input.Placeholder ?? "",
                    InitialValue = input.InitialValue ?? "",
                    BoundFieldId = input.BoundFieldId,
                    IsRequired = input.IsRequired,
                    IsSecure = input.IsSecure
                };
                break;

            case Aether.PluginSDK.UI.FolderPickerContent folder:
                protoWidget.FolderPicker = new FolderPickerWidget
                {
                    Label = folder.Label,
                    BoundFieldId = folder.BoundFieldId,
                    IsRequired = folder.IsRequired
                };
                break;

            case Aether.PluginSDK.UI.FilePickerContent file:
                protoWidget.FilePicker = new FilePickerWidget
                {
                    Label = file.Label,
                    BoundFieldId = file.BoundFieldId,
                    IsRequired = file.IsRequired,
                    AllowedExtensions = file.AllowedExtensions
                };
                break;

            case Aether.PluginSDK.UI.ToggleContent toggle:
                protoWidget.Toggle = new ToggleWidget
                {
                    Label = toggle.Label,
                    BoundFieldId = toggle.BoundFieldId,
                    InitialValue = toggle.InitialValue
                };
                break;

            case Aether.PluginSDK.UI.ContainerContent container:
                var protoContainer = new ContainerWidget
                {
                    Orientation = (ContainerWidget.Types.Orientation)container.Orientation
                };

                foreach (var child in container.Children)
                {
                    var mappedChild = MapWidget(child);
                    if (mappedChild != null)
                    {
                        protoContainer.Children.Add(mappedChild);
                    }
                }

                foreach (var action in container.Actions)
                {
                    protoContainer.Actions.Add(new WidgetAction
                    {
                        Id = action.Id,
                        Label = action.Label,
                        Type = action.Type
                    });
                }

                protoWidget.Container = protoContainer;
                break;

            default:
                // Unknown content type
                return null;
        }

        return protoWidget;
    }
}
