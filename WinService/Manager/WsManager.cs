using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace WinService.Manager;

public class WsManager
{
    private readonly WinService _winService;
    private readonly ClientWebSocket _webSocket;
    private readonly EnergyManager _energyManager;
    private readonly Timer _timer;

    public WsManager(WinService winService)
    {
        _winService = winService;
        _webSocket = new ClientWebSocket();
        _energyManager = new EnergyManager();
        _timer = new Timer { Interval = 500 };
    }

    public async Task Start(CancellationToken token)
    {
        await Connect(_winService.Configuration["Websocket:Endpoint"] ?? "");
        StartCommandReader(token);
        _timer.Elapsed += async (_, _) => await SendHeartbeat();
        _timer.Start();
    }

    public async Task Stop()
    {
        await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        _timer.Stop();
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
            while (!token.IsCancellationRequested)
                if (await ReadTextAsync() is "shutdown")
                    Process.Start("shutdown", "/s /f /t 0");
        }, token);
    }

    private async Task SendHeartbeat()
    {
        if (_winService is not { Computer: not null, Room: not null })
            return;

        _energyManager.UpdateValues();
        var heartbeat = new Heartbeat
        {
            ComputerId = _winService.Computer.ComputerId,
            Name = Environment.MachineName,
            Type = "Heartbeat",
            Room = _winService.Room.RoomId,
            UpTime = DateTime.Now.AddMilliseconds(-1 * Environment.TickCount64),
            Data = new Data
            {
                Power = _energyManager.GetPowerUsage(),
                CpuUsage = _energyManager.GetCpuUsages(),
                RamUsage = _energyManager.GetRamUsage(),
                DiskUsages = _energyManager.GetDiskUsages(),
                EthernetUsages = _energyManager.GetEthernetUsages()
            }
        };

        var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(heartbeat)));
        await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
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
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, _webSocket.CloseStatusDescription, CancellationToken.None);
                return null; // return null if close
            }
        } while (!receiveResult.EndOfMessage);

        return text.ToString();
    }

    private class Heartbeat
    {
        public int ComputerId { get; set; }
        public string Type { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Room { get; set; }
        public DateTime UpTime { get; set; }
        public Data? Data { get; set; }
    }

    private class Data
    {
        public float Power { get; set; }
        public float RamUsage { get; set; }
        public List<float>? CpuUsage { get; set; }
        public List<float>? DiskUsages { get; set; }
        public List<Dictionary<string, float>>? EthernetUsages { get; set; }
    }
}