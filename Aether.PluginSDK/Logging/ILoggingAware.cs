using Serilog;

namespace Aether.PluginSDK.Logging;

public interface ILoggingAware
{
    void SetLogger(ILogger logger);
}
