using System.Reflection;
using System.Runtime.Loader;

namespace Aether.Backend.Plugins;

/// <summary>
/// Custom AssemblyLoadContext for loading plugins in isolation with collectibility support
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Explicitly ignore shared assemblies to ensure they are loaded from the default context
        if (assemblyName.Name == "Aether.PluginSDK" || 
            assemblyName.Name == "Serilog" ||
            assemblyName.Name == "Serilog.Sinks.File" ||
            assemblyName.Name == "Google.Protobuf") 
        {
            return null;
        }

        // Try to resolve from plugin directory first
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // Fall back to default load context for shared assemblies
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
