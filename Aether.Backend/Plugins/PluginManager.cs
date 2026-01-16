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
    private ISessionManager? _sessionManager;

    public PluginManager(string pluginDirectory, ILogger logger)
    {
        _pluginDirectory = pluginDirectory;
        _logger = logger;
    }

    /// <summary>
    /// Set the session manager for injection into plugins that implement ISessionAware
    /// </summary>
    public void SetSessionManager(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;

        // Inject into already-loaded plugins
        foreach (var loaded in _loadedPlugins)
        {
            if (loaded.Plugin is ISessionAware sessionAware)
            {
                sessionAware.SetSessionManager(sessionManager);
                _logger.Debug("Injected session manager for plugin: {Name}", loaded.Plugin.Name);
            }
        }
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
        foreach (var type in assembly.GetExportedTypes())
        {
            if (type.IsInterface || type.IsAbstract) continue;

            // Strict IPlugin check - everything must be a plugin
            if (!typeof(IPlugin).IsAssignableFrom(type)) continue;

            try
            {
                // Create Singleton Instance
                var instance = (IPlugin)Activator.CreateInstance(type)!;

                // 2. Inject Dependencies
                if (instance is Aether.PluginSDK.Logging.ILoggingAware loggingAware)
                {
                    var pluginLogger = _logger
                        .ForContext("PluginName", instance.Name)
                        .ForContext(type);
                    loggingAware.SetLogger(pluginLogger);
                    _logger.Debug("Injected logger for plugin: {Name}", instance.Name);
                }

                if (instance is Aether.PluginSDK.Storage.IStorageAware storageAware)
                {
                    var storage = new Aether.Backend.Services.PluginStorageService(instance.Name);
                    storageAware.SetStorage(storage);
                    _logger.Debug("Injected storage for plugin: {Name}", instance.Name);
                }

                if (instance is ISessionAware sessionAware && _sessionManager != null)
                {
                    sessionAware.SetSessionManager(_sessionManager);
                    _logger.Debug("Injected session manager for plugin: {Name}", instance.Name);
                }

                // 3. Platform Check
                if (instance.SupportedPlatforms != null && instance.SupportedPlatforms.Any())
                {
                    bool isSupported = false;
                    foreach (var platform in instance.SupportedPlatforms)
                    {
                        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Create(platform.ToUpper())))
                        {
                            isSupported = true;
                            break;
                        }
                    }

                    if (!isSupported)
                    {
                        _logger.Information("Skipping plugin {Name}: Not supported on current platform", instance.Name);
                        continue;
                    }
                }

                // 4. Register Capabilities
                var importer = instance as ILibraryImporter;
                var provider = instance as IMetadataProvider;

                // Check inheritance compatibility (sanity check)
                if (instance is ILibraryImporter && importer == null)
                    _logger.Warning("Plugin {Name} implements ILibraryImporter but cast failed.", instance.Name);

                // Add to LoadedPlugins
                var existing = _loadedPlugins.FirstOrDefault(p => p.Plugin?.Name == instance.Name);

                if (existing != null)
                {
                    // Merge capabilities
                    if (existing.Plugin == null) existing.Plugin = instance;
                    if (existing.LibraryImporter == null) existing.LibraryImporter = importer;
                    if (existing.MetadataProvider == null) existing.MetadataProvider = provider;
                }
                else
                {
                    _loadedPlugins.Add(new LoadedPlugin(loadContext, instance, importer, provider));
                }

                _logger.Information("Loaded Plugin '{Name}' (Importer={IsImporter}, Metadata={IsMetadata})",
                    instance.Name, importer != null, provider != null);

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to instantiate plugin type {Type} from {Path}", type.FullName, dllPath);
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
        public IPlugin? Plugin { get; set; }
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
