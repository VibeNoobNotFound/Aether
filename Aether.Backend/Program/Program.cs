using Serilog;

namespace Aether.Backend;

public static class Program
{
    public static void Run(string[] args)
    {
        LoggingConfig.Initialize();
        
        try
        {
            var app = AppBuilder.Build(args);
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
