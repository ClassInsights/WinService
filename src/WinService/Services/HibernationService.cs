using Microsoft.Extensions.Hosting;
using WinService.Manager;

namespace WinService.Services;

public class HibernationService: BackgroundService
{
    private readonly PeriodicTimer _timer = new (TimeSpan.FromMinutes(20));
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            PowerManager.PreventSleep();
        } while (!stoppingToken.IsCancellationRequested && await _timer.WaitForNextTickAsync(stoppingToken));
    }
}