using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace WinService.Manager;

public class WsManager
{
    private readonly EnergyManager _energyManager;
    private readonly Timer _timer;
    private readonly WinService _winService;
    private ClientWebSocket _webSocket;

    public WsManager(WinService winService)
    {
        _winService = winService;
        _webSocket = new ClientWebSocket();
        _energyManager = new EnergyManager();
        _timer = new Timer { Interval = 500 };
    }

    public async Task Start(CancellationToken token)
    {
        if (_winService.Configuration["Websocket:Endpoint"] is not { } endpoint)
            throw new Exception("Websocket:Endpoint is missing in appsettings.json!");

        try
        {
            await Connect(endpoint);
            StartCommandReader(token);
            _timer.Elapsed += async (_, _) => await SendHeartbeat();
            _timer.Start();
        }
        catch (Exception e)
        {
            Logger.Error($"Error occured in WsManager: {e.Message}");
            await Task.Delay(5000, token);
            await Reconnect(token);
        }
    }

    private async Task Reconnect(CancellationToken token)
    {
        Logger.Log("Reconnecting WebSocket ...");
        await Stop();
        _webSocket = new ClientWebSocket();
        await Start(token);
    }

    public async Task Stop()
    {
        _timer.Stop();
        await Close();
        _webSocket.Abort();
    }

    private async Task Close()
    {
        if(_webSocket.State is not WebSocketState.Closed and not WebSocketState.Aborted)
            await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
    }

    private async Task Connect(string endpoint)
    {
        _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_winService.Api.JwtToken}");
        await _webSocket.ConnectAsync(new Uri(endpoint), CancellationToken.None);
        Logger.Log("Connected to Websocket!");
    }

    private void StartCommandReader(CancellationToken token)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var command = await ReadTextAsync();
                    Logger.Log($"Received '{command}' command!");
                    switch (command)
                    {
                        case "shutdown":
                            Process.Start("shutdown", "/s /f /t 0");
                            break;
                        case "restart":
                            Process.Start("shutdown", "/r /f /t 0");
                            break;
                        case "logoff":
                            Process.Start("shutdown", "/l");
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error in CommandReader: {e.Message}");
                await Task.Delay(5000, token);
                await Reconnect(token);
            }
        }, token);
        Logger.Log("Started Websocket Command Reader!");
    }

    private async Task SendHeartbeat()
    {
        if (_winService is not { Computer: not null, Room: not null })
            return;
        
        if (_webSocket.State != WebSocketState.Open) return;

        try
        {
            _energyManager.UpdateValues();
            var heartbeat = new Heartbeat(Name: Environment.MachineName, Type: "Heartbeat", Room: _winService.Room.RoomId,
                UpTime: DateTime.Now.AddMilliseconds(-1 * Environment.TickCount64),
                ComputerId: _winService.Computer.ComputerId,
                Data: new Data(CpuUsage: _energyManager.GetCpuUsages(),
                    Power: _energyManager.GetPowerUsage(), EthernetUsages: _energyManager.GetEthernetUsages(),
                    RamUsage: _energyManager.GetRamUsage(), DiskUsages: _energyManager.GetDiskUsages()));

            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(heartbeat)));
            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception e)
        {
            Logger.Error($"Error while sending Heartbeat: {e.Message}");
        }
    }

    private async Task<string?> ReadTextAsync()
    {
        var buffer = new byte[8192];
        var text = new StringBuilder();

        WebSocketReceiveResult receiveResult;
        do
        {
            receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (receiveResult.MessageType != WebSocketMessageType.Close)
            {
                text.Append(Encoding.UTF8.GetString(new ArraySegment<byte>(buffer, 0, receiveResult.Count)));
            }
            else
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, _webSocket.CloseStatusDescription,
                    CancellationToken.None);
                return null; // return null if close
            }
        } while (!receiveResult.EndOfMessage);

        return text.ToString();
    }

    private record Heartbeat(int ComputerId, string Type, string Name, int Room, DateTime UpTime, Data? Data);

    private record Data(float Power, float RamUsage, List<float>? CpuUsage, List<float>? DiskUsages,
        List<Dictionary<string, float>>? EthernetUsages);
}