namespace Behide.GodotInterop;

using Godot;

using System;
using Microsoft.Extensions.Logging;

sealed class GodotLogger : ILogger
{
    public GodotLogger() { }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Information:
            case LogLevel.None: GD.Print($"{formatter(state, exception)}"); break;
            case LogLevel.Debug: GD.Print($"DBG: {formatter(state, exception)}"); break;
            case LogLevel.Warning: GD.Print($"WARN: {formatter(state, exception)}"); break;
            case LogLevel.Error: GD.PrintErr($"{formatter(state, exception)}"); break;
            case LogLevel.Critical: GD.PrintErr($"CRITICAL: {formatter(state, exception)}"); break;
        }
    }
}

class GodotLoggerProvider : ILoggerProvider
{
    private ILogger? logger;

    public ILogger CreateLogger(string categoryName) => new GodotLogger();

    public void Dispose()
    {
        if (logger == null) return;
        ((IDisposable)logger).Dispose();
    }
}
