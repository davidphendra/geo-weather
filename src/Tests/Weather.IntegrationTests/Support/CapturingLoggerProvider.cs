using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace Weather.IntegrationTests.Support;

/// <summary>Captures structured log records in memory so tests can assert on startup logging offline.</summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<CapturedLog> _logs = new();

    public IReadOnlyCollection<CapturedLog> Logs => _logs.ToArray();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(_logs);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(ConcurrentQueue<CapturedLog> logs) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Dictionary<string, object?> properties = new(StringComparer.Ordinal);

            if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                foreach (KeyValuePair<string, object?> pair in pairs)
                {
                    properties[pair.Key] = pair.Value;
                }
            }

            logs.Enqueue(new CapturedLog(logLevel, formatter(state, exception), properties));
        }
    }
}

/// <summary>A single captured log record: level, formatted message, and structured properties.</summary>
internal sealed record CapturedLog(LogLevel Level, string Message, IReadOnlyDictionary<string, object?> Properties);
