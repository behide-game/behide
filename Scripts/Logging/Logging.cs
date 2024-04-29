namespace Behide.Log;

using Serilog;
using Serilog.Events;

public static class Logging
{
    public static void ConfigureLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Minimum level for SignalR logs
            .WriteTo.Godot("[{Timestamp:HH:mm:ss} {Level:u3}] [{Tag:u3}] {Message:lj}")
            .CreateLogger();
    }
}
