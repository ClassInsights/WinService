using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NodaTime;
using WinService.Models;

namespace WinService.Logging;

public class HttpBatchLogger(string categoryName, IClock clock, BlockingCollection<ApiModels.ComputerLog> queue)
    : ILogger
{
    private static readonly Dictionary<LogLevel, string> LogLevelStrings = new()
    {
        [LogLevel.Trace] = "Trace",
        [LogLevel.Debug] = "Debug", 
        [LogLevel.Information] = "Information",
        [LogLevel.Warning] = "Warning",
        [LogLevel.Error] = "Error",
        [LogLevel.Critical] = "Critical"
    };

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;
    
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var levelString = LogLevelStrings.TryGetValue(logLevel, out var cached) 
            ? cached 
            : logLevel.ToString();

        var payload = new ApiModels.ComputerLog
        {
            Timestamp = clock.GetCurrentInstant(),
            Level = levelString,
            Category = categoryName,
            Message = formatter(state, exception),
            Details = exception?.ToString()
        };

        queue.TryAdd(payload, TimeSpan.FromMilliseconds(5));
    }
}
