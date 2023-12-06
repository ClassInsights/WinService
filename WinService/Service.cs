using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WinService;

public class Service(ILogger<Service> logger) : BackgroundService
{
    private WinService? _winService;


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _winService = new WinService();
            await _winService.RunAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "{Message}", ex.Message);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_winService is not null)
                await _winService.StopAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "{Message}", ex.Message);
        }
    }
}