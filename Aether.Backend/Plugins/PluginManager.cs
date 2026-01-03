using System.Reflection;
using Aether.PluginSDK;
using Aether.PluginSDK.Library;
using Serilog;

namespace Aether.Backend.Plugins;

/// <summary>
/// Manages dynamic loading and lifecycle of plugins
/// </summary>
public class PluginManager : IDisposable
{
    private readonly List<LoadedPlugin> _loadedPlugins = new();
    private readonly ILogger _logger;
    private readonly string _pluginDirectory;

    public PluginManager(string pluginDirectory, ILogger logger)
    {
        _pluginDirectory = pluginDirectory;
        _logger = logger;
    }

    /// <summary>
    /// Load all plugins from the plugins directory
    /// </summary>
    public void LoadPlugins()
    {
        if (!Directory.Exists(_pluginDirectory))
        {
            _logger.Warning("Plugin directory does not exist: {PluginDirectory}", _pluginDirectory);
            Directory.CreateDirectory(_pluginDirectory);
            return;
        }

        var pluginDlls = Directory.GetFiles(_pluginDirectory, "Aether.Importers.*.dll");
        _logger.Information("Found {Count} potential plugin DLLs", pluginDlls.Length);

        foreach (var dllPath in pluginDlls)
        {
            try
            {
                LoadPlugin(dllPath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load plugin from {Path}", dllPath);
            }
        }

        _logger.Information("Successfully loaded {Count} plugins", _loadedPlugins.Count);
    }

    private void LoadPlugin(string dllPath)
    {
        var loadContext = new PluginLoadContext(dllPath);
        var assembly = loadContext.LoadFromAssemblyPath(dllPath);

        _logger.Debug("Loaded assembly {AssemblyName} from {Path}", assembly.FullName, dllPath);

        // Find all types implementing our plugin interfaces
        foreach (var type in assembly.GetTypes())
        {
            // Check for IPlugin
            if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            {
                var plugin = (IPlugin)Activator.CreateInstance(type)!;
                
                // Inject storage if plugin implements IStorageAware
                if (plugin is Aether.PluginSDK.Storage.IStorageAware storageAware)
                {
                    var storage = new Aether.Backend.Services.PluginStorageService(plugin.Name);
                    storageAware.SetStorage(storage);
                    _logger.Debug("Injected storage for plugin: {Name}", plugin.Name);
                }
                
                // Check Platform Support
                if (plugin.SupportedPlatforms != null && plugin.SupportedPlatforms.Any())
                {
                    bool isSupported = false;
                    foreach (var platform in plugin.SupportedPlatforms)
                    {
                        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Create(platform.ToUpper())))
                        {
                            isSupported = true;
                            break;
                        }
                    }
                    
                    if (!isSupported)
                    {
                        _logger.Information("Skipping plugin {Name}: Not supported on current platform", plugin.Name);
                        continue;
                    }
                }

                _loadedPlugins.Add(new LoadedPlugin(loadContext, plugin, null, null));
                _logger.Information("Loaded Plugin: {Name}", plugin.Name);
            }

            // Check for ILibraryImporter
            if (typeof(ILibraryImporter).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            {
                var importer = (ILibraryImporter)Activator.CreateInstance(type)!;

                // Update or add to loaded plugins
                var existing = _loadedPlugins.FirstOrDefault(p => p.Plugin?.Name == importer.Name);
                if (existing != null)
                {
                    existing.LibraryImporter = importer;
                }
                else
                {
                    _loadedPlugins.Add(new LoadedPlugin(loadContext, null, importer, null));
                }

                _logger.Information("Loaded Library Importer: {Name} v{Version}", importer.Name, importer.Version);
            }

            // Check for IMetadataProvider
            if (typeof(IMetadataProvider).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            {
                var provider = (IMetadataProvider)Activator.CreateInstance(type)!;

                var existing = _loadedPlugins.FirstOrDefault(p => p.Plugin?.Name == provider.Name);
                if (existing != null)
                {
                    existing.MetadataProvider = provider;
                }
                else
                {
                    _loadedPlugins.Add(new LoadedPlugin(loadContext, null, null, provider));
                }

                _logger.Information("Loaded Metadata Provider: {Name}", provider.Name);
            }
        }
    }

    /// <summary>
    /// Get all loaded library importers
    /// </summary>
    public IEnumerable<ILibraryImporter> GetLibraryImporters()
    {
        return _loadedPlugins
            .Where(p => p.LibraryImporter != null)
            .Select(p => p.LibraryImporter!);
    }

    /// <summary>
    /// Get all loaded metadata providers
    /// </summary>
    public IEnumerable<IMetadataProvider> GetMetadataProviders()
    {
        return _loadedPlugins
            .Where(p => p.MetadataProvider != null)
            .Select(p => p.MetadataProvider!);
    }

    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    public IEnumerable<IPlugin> GetPlugins()
    {
        return _loadedPlugins
            .Where(p => p.Plugin != null)
            .Select(p => p.Plugin!);
    }

    public void Dispose()
    {
        foreach (var plugin in _loadedPlugins)
        {
            plugin.LoadContext.Unload();
        }
        _loadedPlugins.Clear();

        // Trigger garbage collection to ensure unloading
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private class LoadedPlugin
    {
        public PluginLoadContext LoadContext { get; }
        public IPlugin? Plugin { get; }
        public ILibraryImporter? LibraryImporter { get; set; }
        public IMetadataProvider? MetadataProvider { get; set; }

        public LoadedPlugin(
            PluginLoadContext loadContext,
            IPlugin? plugin,
            ILibraryImporter? libraryImporter,
            IMetadataProvider? metadataProvider)
        {
            LoadContext = loadContext;
            Plugin = plugin;
            LibraryImporter = libraryImporter;
            MetadataProvider = metadataProvider;
        }
    }
}
