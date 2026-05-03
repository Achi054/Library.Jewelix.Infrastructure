using Microsoft.Extensions.Logging;

using Serilog.Context;
using Serilog.Events;

namespace Jewelix.Logging;

/// <summary>
/// Adapts <see cref="ILogger{TCategoryName}"/> calls to Serilog while keeping
/// the contextual <c>Log.ForContext&lt;T&gt;</c> binding so each log event is
/// stamped with the calling type as <c>SourceContext</c>.
/// </summary>
public sealed class SerilogLogger<T> : ILogger<T>
{
    // Resolved lazily on every call so we always observe the current Log.Logger
    // (set by UseJewelixLogger) instead of whatever was current when this singleton
    // was constructed — avoids the bootstrap-order trap where DI resolves a logger
    // before Serilog has been configured.
    private static Serilog.ILogger Logger => Serilog.Log.ForContext<T>();

    /// <summary>
    /// Begins a logical scope by pushing <paramref name="state"/> onto the Serilog
    /// <see cref="LogContext"/> as a <c>Scope</c> property. Dispose the returned
    /// handle to pop the scope.
    /// </summary>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => LogContext.PushProperty("Scope", state);

    /// <summary>
    /// Returns <see langword="false"/> when <paramref name="logLevel"/> is
    /// <see cref="LogLevel.None"/> or when the underlying Serilog logger has
    /// that level disabled; <see langword="true"/> otherwise.
    /// </summary>
    public bool IsEnabled(LogLevel logLevel)
        => logLevel != LogLevel.None && Logger.IsEnabled(MapLogLevel(logLevel));

    /// <summary>
    /// Formats <paramref name="state"/> via <paramref name="formatter"/> and writes
    /// the resulting message (and optional <paramref name="exception"/>) to the
    /// Serilog pipeline at the mapped log level. No-ops when
    /// <paramref name="logLevel"/> is <see cref="LogLevel.None"/> or the underlying
    /// Serilog level is disabled.
    /// </summary>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.None)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(formatter);

        var serilogLevel = MapLogLevel(logLevel);
        if (!Logger.IsEnabled(serilogLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception is null)
        {
            return;
        }

        Logger.Write(serilogLevel, exception, message);
    }

    private static LogEventLevel MapLogLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical => LogEventLevel.Fatal,
        _ => LogEventLevel.Verbose,
    };
}
