using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using WinService.Interfaces;
using WinService.Models;

namespace WinService.Services;

public class LogSenderService(BlockingCollection<ApiModels.ComputerLog> queue, IApiManager apiManager)
    : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<ApiModels.ComputerLog>();
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (!stoppingToken.IsCancellationRequested)
        {
            while (queue.TryTake(out var log))
            {
                log.ComputerId = apiManager.Computer?.ComputerId;
                batch.Add(log);
                if (batch.Count >= 20) break; // Flush when 20 logs accumulated
            }

            if (batch.Count > 0 && (batch.Count >= 20 || await timer.WaitForNextTickAsync(stoppingToken)))
            {
                try
                {
                    await apiManager.BatchLogs(batch);
                }
                catch (Exception)
                {
                    // Failed to log to HTTP ...
                }
                batch.Clear();
            }
        }
    }
}