namespace Behide.Log;

using Godot;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

public static class Logging
{
    private static readonly string consoleOutputFormat = "[{Timestamp:HH:mm:ss} {Level:u3}] [{Tag:u3}] {Message:lj}";
    private static readonly string logFileOutputFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{Tag}] {Message:lj}{NewLine}{Exception}";

    public static void ConfigureLogger()
    {
        var now = System.DateTimeOffset.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var logFilePath = ProjectSettings.GlobalizePath($"user://logs/log-{now}.txt");
        var jsonLogFilePath = ProjectSettings.GlobalizePath($"user://logs/log-json-{now}.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Minimum level for SignalR logs
            .WriteTo.Logger(cl => cl // Console logger
                .MinimumLevel.Verbose()
                .WriteTo.Godot(consoleOutputFormat)
            )
            .WriteTo.Logger(cl => cl // File logger
                .WriteTo.File(logFilePath, outputTemplate: logFileOutputFormat)
                .WriteTo.File(new JsonFormatter(), jsonLogFilePath)
            )
            .CreateLogger();
    }
}
