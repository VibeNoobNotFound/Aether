using Serilog;

namespace Aether.PluginSDK.Logging;

public interface ILoggingAware : IPlugin
{
    void SetLogger(ILogger logger);
}
