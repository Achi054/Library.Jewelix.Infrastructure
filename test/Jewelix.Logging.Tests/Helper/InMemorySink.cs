using Serilog.Core;
using Serilog.Events;

namespace Jewelix.Logging.Tests.Helper;

/// <summary>
/// Captures emitted Serilog events in memory so tests can assert against them.
/// Thread-safe so middleware tests on TestServer don't race with the test thread.
/// </summary>
internal sealed class InMemorySink : ILogEventSink
{
    private readonly object _gate = new();
    private readonly List<LogEvent> _events = new();

    /// <summary>
    /// Returns a snapshot of all log events captured since this sink was created.
    /// </summary>
    public IReadOnlyList<LogEvent> Events
    {
        get
        {
            lock (_gate)
            {
                return _events.ToList();
            }
        }
    }

    /// <summary>
    /// Appends <paramref name="logEvent"/> to the in-memory event list.
    /// </summary>
    public void Emit(LogEvent logEvent)
    {
        lock (_gate)
        {
            _events.Add(logEvent);
        }
    }
}
