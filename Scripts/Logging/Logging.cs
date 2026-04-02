using Godot;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Behide.Logging;

public static class Logging
{
    private const string consoleOutputFormat = "[{Timestamp:HH:mm:ss} {Level:u3}] [{Tag:u3}] {Message:lj}";
    private const string logFileOutputFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{Tag}] {Message:lj}{NewLine}{Exception}";

    public static void ConfigureLogger()
    {
        var logToGodotSink = OS.GetCmdlineArgs().Contains("--log-to-godot-sink");

        var now = DateTimeOffset.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var logFilePath = ProjectSettings.GlobalizePath($"user://logs/log-{now}.txt");
        var jsonLogFilePath = ProjectSettings.GlobalizePath($"user://logs/log-json-{now}.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose() // TODO: change for production
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Minimum level for SignalR logs
            // Console logger
            .WriteTo.Logger(cl =>
            {
                if (logToGodotSink)
                    cl
                    .MinimumLevel.Debug()
                    .WriteTo.Godot(consoleOutputFormat);
                else
                    cl.WriteTo.Console(outputTemplate: consoleOutputFormat + System.Environment.NewLine);
            })
            // File logger
            .WriteTo.Logger(cl => cl
                .WriteTo.File(logFilePath, outputTemplate: logFileOutputFormat)
                .WriteTo.File(new JsonFormatter(), jsonLogFilePath)
            )
            .CreateLogger();
    }
}
