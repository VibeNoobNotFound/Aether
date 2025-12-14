using Aether.Protos;
using Aether.Backend.Plugins;
using Aether.Backend.Data;
using Grpc.Core;
using Microsoft.Extensions.Logging;

using Aether.PluginSDK;

namespace Aether.Backend.Services;

public partial class AetherGrpcService : AetherOrchestrator.AetherOrchestratorBase
{
    private readonly ILogger<AetherGrpcService> _logger;
    private readonly PluginManager _pluginManager;
    private readonly LibraryDatabase _database;

    public AetherGrpcService(
        ILogger<AetherGrpcService> logger,
        PluginManager pluginManager,
        LibraryDatabase database)
    {
        _logger = logger;
        _pluginManager = pluginManager;
        _database = database;
    }

    public override Task<PluginList> GetPlugins(Empty request, ServerCallContext context)
    {
        var response = new PluginList();

        // Add Library Importers
        foreach (var importer in _pluginManager.GetLibraryImporters())
        {
            response.Plugins.Add(new PluginInfo
            {
                Name = importer.Name,
                Version = importer.Version,
                Author = "Unknown",
                IsImporter = true,
                IsMetadataProvider = false
            });
        }

        // Add pure plugins if any (that aren't importers)
        foreach (var plugin in _pluginManager.GetPlugins())
        {
            // Simple de-duplication based on name
            var exists = response.Plugins.Any(p => p.Name == plugin.Name);
            if (!exists)
            {
                response.Plugins.Add(new PluginInfo
                {
                    Name = plugin.Name,
                    Version = "1.0.0",
                    Author = "Unknown",
                    IsImporter = false, // defaults
                    IsMetadataProvider = false
                });
            }
        }

        return Task.FromResult(response);
    }

    public override Task<WidgetList> GetSetupWidgets(PluginName request, ServerCallContext context)
    {
        var response = new WidgetList();

        // Search importers
        var importer = _pluginManager.GetLibraryImporters().FirstOrDefault(p => p.Name == request.Name);
        if (importer != null)
        {
            foreach (var widget in importer.GetSetupWidgets())
            {
                response.Widgets.Add(MapWidget(widget));
            }
            return Task.FromResult(response);
        }

        // Search plugins
        var plugin = _pluginManager.GetPlugins().FirstOrDefault(p => p.Name == request.Name);
        if (plugin != null)
        {
            foreach (var widget in plugin.GetSetupWidgets())
            {
                response.Widgets.Add(MapWidget(widget));
            }
            return Task.FromResult(response);
        }

        return Task.FromResult(response);
    }

    private static PluginWidget MapWidget(Aether.PluginSDK.UI.Widget widget)
    {
        return new PluginWidget
        {
            PluginId = widget.PluginId,
            Title = widget.Title,
            LayoutJson = widget.LayoutJson,
            SortOrder = widget.SortOrder
        };
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

            await plugin.OnWidgetAction(request.ActionId, request.PayloadJson);
            return new OperationStatus { Success = true, Message = "Action executed." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing plugin action");
            return new OperationStatus { Success = false, Message = ex.Message };
        }
    }
}



