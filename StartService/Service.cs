namespace StartService;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Service : BackgroundService
{
    private readonly ILogger<Service> _logger;

    public Service(ILogger<Service> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        try
        {
            await new StartService().RunAsync(token);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "{Message}", ex.Message);
        }
    }
}