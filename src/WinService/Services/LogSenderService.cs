using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WinService.Interfaces;
using WinService.Models;

namespace WinService.Services;

public class LogSenderService(
    BlockingCollection<ApiModels.ComputerLog> queue,
    IApiManager apiManager,
    ILogger<LogSenderService> logger)
    : BackgroundService
{
    private const int MaxBatchSize = 50;
    private const int MinBatchSize = 5;
    private readonly TimeSpan _maxWaitTime = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<ApiModels.ComputerLog>(MaxBatchSize);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var batchFilled = CollectBatch(batch, stoppingToken);
                
                if (batch.Count > 0)
                {
                    await ProcessBatch(batch);
                    batch.Clear();
                }
                
                if (!batchFilled)
                {
                    await Task.Delay(100, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in log sender service");
                await Task.Delay(1000, stoppingToken);
            }
        }
        
        if (batch.Count > 0)
        {
            await ProcessBatch(batch);
        }
    }

    private bool CollectBatch(List<ApiModels.ComputerLog> batch, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.Add(_maxWaitTime);
        var hasLogs = false;

        while (batch.Count < MaxBatchSize && DateTime.UtcNow < deadline)
        {
            if (queue.TryTake(out var log, 50, cancellationToken))
            {
                if (apiManager.Computer?.ComputerId == null)
                    continue;
                log.ComputerId = apiManager.Computer.ComputerId;
                batch.Add(log);
                hasLogs = true;
            }
            else if (batch.Count >= MinBatchSize || cancellationToken.IsCancellationRequested)
                break;
        }

        return hasLogs;
    }

    private async Task ProcessBatch(List<ApiModels.ComputerLog> batch)
    {
        if (apiManager.Computer?.ComputerId == null)
        {
            logger.LogWarning("Computer not registered, dropping {LogCount} logs", batch.Count);
            return;
        }

        try
        {
            await apiManager.BatchLogs(batch);
            logger.LogDebug("Successfully sent {LogCount} logs", batch.Count);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to send {LogCount} logs - network error", batch.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send {LogCount} logs", batch.Count);
        }
    }
}
