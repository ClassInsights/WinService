using System.Net;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace WinService.Manager;

public class WsManager
{
    private const string Endpoint = "wss://srv-iis.projekt.lokal/ws/pc";
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

    public async Task Start()
    {
        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        await Connect(Endpoint);

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
        await _webSocket.ConnectAsync(new Uri(endpoint), CancellationToken.None);
        Logger.Log("Connected to Websocket!");
    }

    private async Task SendHeartbeat()
    {
        var heartbeat = new Heartbeat
        {
            Name = Environment.MachineName,
            Type = "Heartbeat",
            Room = _winService.Room.Id,
            Data = new Data
            {
                Power = _energyManager.GetCpuEnergy()
            }
        };

        var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(heartbeat)));
        await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private class Heartbeat
    {
        public string Type { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Room { get; set; }
        public Data? Data { get; set; }
    }

    private class Data
    {
        public float Power { get; set; }
        public string? Token { get; set; }
    }
}