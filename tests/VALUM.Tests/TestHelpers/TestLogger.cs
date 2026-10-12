using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace VALUM.Tests.TestHelpers
{
    public class TestLogger : ILogger
    {
        public readonly List<LogEntry> LogEntries = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NoOpDisposable();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var formattedMessage = formatter(state, exception);
            LogEntries.Add(new LogEntry
            {
                LogLevel = logLevel,
                FormattedMessage = formattedMessage,
                Exception = exception,
                State = state
            });
        }

        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    public class TestLogger<T> : TestLogger, ILogger<T>
    {
    }

    public class LogEntry
    {
        public LogLevel LogLevel { get; set; }
        public string FormattedMessage { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public object? State { get; set; }
    }
}
