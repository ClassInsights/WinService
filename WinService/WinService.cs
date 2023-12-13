using Microsoft.Extensions.Configuration;
using WinService.Manager;
using WinService.Models;

namespace WinService;

public class WinService
{
    public const string Version = "beta-v1.2.4";
    private readonly HeartbeatManager _heartbeatManager;
    private readonly ShutdownManager _shutdownManager;
    private readonly WsManager _wsManager;
    public readonly Api Api;

    public readonly IConfigurationRoot Configuration =
        new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();

    public ApiModels.Computer? Computer;
    public ApiModels.Room? Room;
    
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
        await Api.Authorize();

#if DEBUG
        Room = await Api.GetRoomAsync("DV2");
        Computer = await Api.GetComputerAsync("OG2-DV2");
#else
        Room = await Api.GetRoomAsync(Environment.MachineName);
        Computer = await Api.GetComputerAsync(Environment.MachineName);
#endif
        if (Computer == null || Room == null)
        {
            Logger.Error("Failed to retrieve Room or Computer objects!");
            return;
        }
        try
        {
            await _heartbeatManager.Start(token);
            await Task.WhenAll(_shutdownManager.Start(token), ShutdownManager.CheckLifeSign(token),
                _wsManager.Start(token)); // takes endless unless service stop
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