using System.Linq;
using Behide.Game.Supervisors;
using Godot;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Behide.Logging;

public static class Logging
{
    private const string ConsoleOutputFormat = "[{Timestamp:HH:mm:ss} {Level:u3}] [{Tag:u3}] {Message:lj}";
    private const string LogFileOutputFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{Tag}] {Message:lj}{NewLine}{Exception}";

    public static void ConfigureLogger()
    {
        var logToGodotSink = OS.GetCmdlineArgs().Contains("--log-to-godot-sink");

        var now = System.DateTimeOffset.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var logFilePath = ProjectSettings.GlobalizePath($"user://logs/log-{now}.txt");
        var jsonLogFilePath = ProjectSettings.GlobalizePath($"user://logs/log-json-{now}.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose() // TODO: change for production
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Minimum level for SignalR logs
            .Filter.ByExcluding(logEvent => {
                if (logEvent.Properties.TryGetValue("Tag", out var tag))
                    return tag.ToString() == $"\"{BasicSupervisor.Tag}\"";
                return false;
            })

            // Console logger
            .WriteTo.Logger(cl =>
            {
                if (logToGodotSink)
                    cl
                    .MinimumLevel.Debug()
                    .WriteTo.Godot(ConsoleOutputFormat);
                else
                    cl.WriteTo.Console(outputTemplate: ConsoleOutputFormat + System.Environment.NewLine);
            })
            // File logger
            .WriteTo.Logger(cl => cl
                .WriteTo.File(logFilePath, outputTemplate: LogFileOutputFormat)
                .WriteTo.File(new JsonFormatter(), jsonLogFilePath)
            )
            .CreateLogger();
    }
}
