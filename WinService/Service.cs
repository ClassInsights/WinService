using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WinService;

public class Service : BackgroundService
{
    private readonly ILogger<Service> _logger;
    private WinService? _winService;

    public Service(ILogger<Service> logger)
    {
        _logger = logger;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _winService = new WinService();
            await _winService.RunAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "{Message}", ex.Message);
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
            _logger.LogCritical(ex, "{Message}", ex.Message);
        }
    }
}