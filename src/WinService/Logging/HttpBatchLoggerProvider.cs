using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NodaTime;
using WinService.Models;

namespace WinService.Logging;

public class HttpBatchLoggerProvider(IClock clock, BlockingCollection<ApiModels.ComputerLog> queue) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new HttpBatchLogger(categoryName, clock, queue);
    }

    public void Dispose()
    {
        queue.Dispose();
        GC.SuppressFinalize(this);
    }
}
