using Microsoft.Extensions.Configuration;
using WinService.Manager;
using WinService.Models;

namespace WinService;

public class WinService
{
    public const string Version = "beta-v1.2.6";
    private readonly HeartbeatManager _heartbeatManager;
    private readonly ShutdownManager _shutdownManager;
    private readonly WsManager _wsManager;
    public readonly Api Api;

    public readonly IConfigurationRoot Configuration =
        new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();

    public ApiModels.Computer Computer = null!;
    public ApiModels.Room Room = null!;
    
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
        try
        {
            await Api.Authorize();

#if DEBUG
            var room = await Api.GetRoomAsync("DV2");
            var computer = await Api.GetComputerAsync("OG2-DV2");
#else
            var room = await Api.GetRoomAsync(Environment.MachineName);
            var computer = await Api.GetComputerAsync(Environment.MachineName);
#endif
            if (computer == null || room == null)
            {
                Logger.Error("Failed to retrieve Room or Computer objects!");
                return;
            }

            Computer = computer;
            Room = room;
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to start WinService: {e.Message}");
            await Task.Delay(180000, token); // wait 3 minutes
            await RunAsync(token); // try again
        }
        
        try
        {
            _heartbeatManager.Start(token);
            
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
        _heartbeatManager.Dispose();
        await _wsManager.Stop();
    }
}