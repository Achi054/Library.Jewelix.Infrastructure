using Jewelix.Logging.Tests.Helper;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;

namespace Jewelix.Logging.Tests;

/// <summary>
/// Tests for <see cref="SerilogLogger{T}"/>: log-level mapping from
/// <c>Microsoft.Extensions.Logging</c> to Serilog, exception attachment,
/// <c>SourceContext</c> stamping, and <c>None</c>-level suppression.
/// </summary>
[Collection(SerilogTestCollection.Name)]
public class SerilogLoggerTests : IDisposable
{
    private readonly InMemorySink _sink = new();
    private readonly Serilog.ILogger _previous;

    public SerilogLoggerTests()
    {
        _previous = Log.Logger;
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(_sink)
            .CreateLogger();
    }

    public void Dispose()
    {
        (Log.Logger as IDisposable)?.Dispose();
        Log.Logger = _previous;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void IsEnabled_WithNoneLogLevel_ReturnsFalse()
    {
        var logger = new SerilogLogger<SerilogLoggerTests>();
        logger.IsEnabled(LogLevel.None).ShouldBeFalse();
    }

    [Fact]
    public void Log_WithNoneLogLevel_DoesNotEmitEvent()
    {
        var logger = new SerilogLogger<SerilogLoggerTests>();
        logger.Log(LogLevel.None, default, "should be ignored", null, (s, _) => s);
        _sink.Events.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(LogLevel.Trace, LogEventLevel.Verbose)]
    [InlineData(LogLevel.Debug, LogEventLevel.Debug)]
    [InlineData(LogLevel.Information, LogEventLevel.Information)]
    [InlineData(LogLevel.Warning, LogEventLevel.Warning)]
    [InlineData(LogLevel.Error, LogEventLevel.Error)]
    [InlineData(LogLevel.Critical, LogEventLevel.Fatal)]
    public void Log_WithMelLogLevel_MapsSerilogLevel(LogLevel input, LogEventLevel expected)
    {
        var logger = new SerilogLogger<SerilogLoggerTests>();
        logger.Log(input, default, "msg", null, (s, _) => s);

        _sink.Events.ShouldHaveSingleItem().Level.ShouldBe(expected);
    }

    [Fact]
    public void Log_WithExceptionProvided_AttachesExceptionToEvent()
    {
        var logger = new SerilogLogger<SerilogLoggerTests>();
        var ex = new InvalidOperationException("boom");

        logger.LogError(ex, "failure");

        var ev = _sink.Events.ShouldHaveSingleItem();
        ev.Exception.ShouldBe(ex);
        ev.Level.ShouldBe(LogEventLevel.Error);
    }

    [Fact]
    public void Log_WithGenericTypeParameter_StampsSourceContext()
    {
        var logger = new SerilogLogger<SerilogLoggerTests>();
        logger.LogInformation("hello");

        var ev = _sink.Events.ShouldHaveSingleItem();
        ev.Properties.ShouldContainKey("SourceContext");
        ev.Properties["SourceContext"].ToString().ShouldContain(nameof(SerilogLoggerTests));
    }
}
