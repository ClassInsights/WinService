using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WinService;

public class Service : BackgroundService
{
    private readonly ILogger<Service> _logger;
    private AutoShutdown? _shutdown;

    public Service(ILogger<Service> logger)
    {
        _logger = logger;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _shutdown = new AutoShutdown();
            await _shutdown.RunAsync();
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
            if (_shutdown is not null) await _shutdown.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "{Message}", ex.Message);
        }
    }
}