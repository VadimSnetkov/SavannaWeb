using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace Savanna.Tests.Helpers;

public sealed class TestLogSink : ILogEventSink
{
    public ConcurrentQueue<LogEvent> Events { get; } = new();

    /// Summary:
    /// Captures structured events to verify exception and lifecycle logging.
    public void Emit(LogEvent logEvent) => Events.Enqueue(logEvent);
}
