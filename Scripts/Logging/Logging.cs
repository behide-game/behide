namespace Behide.Log;

using Godot;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Linq;

public static class Logging
{
    private static readonly string consoleOutputFormat = "[{Timestamp:HH:mm:ss} {Level:u3}] [{Tag:u3}] {Message:lj}";
    private static readonly string logFileOutputFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{Tag}] {Message:lj}{NewLine}{Exception}";

    public static void ConfigureLogger()
    {
        var logDirectlyToConsole = OS.GetCmdlineUserArgs().Contains("--log-directly-to-console");

        var now = System.DateTimeOffset.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var logFilePath = ProjectSettings.GlobalizePath($"user://logs/log-{now}.txt");
        var jsonLogFilePath = ProjectSettings.GlobalizePath($"user://logs/log-json-{now}.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose() // Todo: change for production
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Minimum level for SignalR logs

            // Console logger
            .WriteTo.Logger(cl =>
            {
                if (logDirectlyToConsole)
                    cl.WriteTo.Console(outputTemplate: consoleOutputFormat + "\n");
                else
                    cl
                    .MinimumLevel.Information()
                    .WriteTo.Godot(consoleOutputFormat);
            })
            .WriteTo.Logger(cl => cl // File logger
                .WriteTo.File(logFilePath, outputTemplate: logFileOutputFormat)
                .WriteTo.File(new JsonFormatter(), jsonLogFilePath)
            )
            .CreateLogger();
    }
}
