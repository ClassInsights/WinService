using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NodaTime;
using WinService.Models;

namespace WinService.Logging;

public class HttpBatchLogger(string categoryName, IClock clock, BlockingCollection<ApiModels.ComputerLog> queue)
    : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null!;
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        
        if (!IsEnabled(logLevel)) return;

        var payload = new ApiModels.ComputerLog
        {
            Timestamp = clock.GetCurrentInstant(),
            Level = logLevel.ToString(),
            Category = categoryName,
            Message = formatter(state, exception),
            Details = exception?.ToString()
        };

        queue.TryAdd(payload); // Non-blocking
    }
}
