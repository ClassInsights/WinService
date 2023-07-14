using WinService.Manager;
using WinService.Models;

namespace WinService;

public class WinService
{
    private readonly WsManager _wsManager = new ();
    private readonly ShutdownManager _shutdownManager;
    private readonly HeartbeatManager _heartbeatManager;
    public DbModels.TabRooms Room = new();

    public WinService()
    {
        _shutdownManager = new ShutdownManager(this);
        _heartbeatManager = new HeartbeatManager(this);
    }

    public async Task RunAsync(CancellationToken token)
    {
        Logger.Log("Run WinService!");
        Room = await Api.GetRoomAsync(Environment.MachineName);

        try
        {
            _heartbeatManager.Start(token);
            await _shutdownManager.Start(token);
            await _wsManager.Start("wss://srv-iis.projekt.lokal/ws/pc");
        }
        catch (OperationCanceledException)
        {
            Logger.Log("Tasks cancelled!");
        }

        await Task.Delay(-1, token);
    }

    public async Task StopAsync()
    {
        await _wsManager.Stop();
    }
}