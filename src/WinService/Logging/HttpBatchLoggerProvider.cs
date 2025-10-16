using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NodaTime;
using WinService.Models;

namespace WinService.Logging;

public class HttpBatchLoggerProvider(IClock clock, BlockingCollection<ApiModels.ComputerLog> queue)
    : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, HttpBatchLogger> _loggers = new();

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, 
            name => new HttpBatchLogger(name, clock, queue));
    }

    public void Dispose()
    {
        _loggers.Clear();
        GC.SuppressFinalize(this);
    }
}