using Microsoft.Extensions.Configuration;
using WinService.Manager;
using WinService.Models;

namespace WinService;

public class WinService
{
    private readonly WsManager _wsManager;
    private readonly ShutdownManager _shutdownManager;
    private readonly HeartbeatManager _heartbeatManager;
    public readonly Api Api;
    public readonly IConfigurationRoot Configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
    public DbModels.TabRooms Room = new();

    public WinService()
    {
        Api = new Api(this);
        _shutdownManager = new ShutdownManager(this);
        _heartbeatManager = new HeartbeatManager(this);
        _wsManager = new WsManager(this);
    }

    public async Task RunAsync(CancellationToken token)
    {
        Logger.Log("Run WinService!");
        Room = await Api.GetRoomAsync(Environment.MachineName);

        try
        {
            _heartbeatManager.Start(token);
            await Task.WhenAll(_shutdownManager.Start(token), _wsManager.Start()); // takes endless unless service stop
        }
        catch (OperationCanceledException)
        {
            Logger.Log("Tasks cancelled!");
        }
    }

    public async Task StopAsync()
    {
        await _wsManager.Stop();
    }
}