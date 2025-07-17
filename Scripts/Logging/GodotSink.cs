using System;
using System.IO;
using Godot;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Behide.Logging;

public class GodotSink(string outputTemplate, IFormatProvider? formatProvider) : ILogEventSink
{
    private readonly MessageTemplateTextFormatter formatter = new(outputTemplate, formatProvider);

    public void Emit(LogEvent logEvent)
    {
        using TextWriter writer = new StringWriter();
        formatter.Format(logEvent, writer);
        writer.Flush();

        string color = logEvent.Level switch
        {
            LogEventLevel.Debug => Colors.SpringGreen.ToHtml(),
            LogEventLevel.Information => Colors.Cyan.ToHtml(),
            LogEventLevel.Warning => Colors.Yellow.ToHtml(),
            LogEventLevel.Error => Colors.Red.ToHtml(),
            LogEventLevel.Fatal => Colors.Purple.ToHtml(),
            _ => Colors.LightGray.ToHtml(),
        };

        foreach (string line in writer.ToString()?.Split('\n') ?? [])
            GD.PrintRich($"[color=#{color}]{line}[/color]");

        if (logEvent.Exception is null) return;

        if (logEvent.Level >= LogEventLevel.Error)
            GD.PushError(logEvent.Exception);
        else
            GD.PushWarning(logEvent.Exception);
    }
}

public static class GodotSinkExtensions
{
    private const string DefaultGodotSinkOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}";

    public static LoggerConfiguration Godot(
        this LoggerSinkConfiguration configuration,
        string outputTemplate = DefaultGodotSinkOutputTemplate,
        IFormatProvider? formatProvider = null)
    {
        return configuration.Sink(new GodotSink(outputTemplate, formatProvider));
    }
}
