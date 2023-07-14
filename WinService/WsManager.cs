using System.Net;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace WinService;

public class WsManager
{
    private readonly ClientWebSocket _webSocket;
    private readonly EnergyManager _energyManager;

    public WsManager()
    {
        _webSocket = new ClientWebSocket();
        _energyManager = new EnergyManager();
    }

    public async Task Start(string endpoint)
    {
        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        await Connect(endpoint);
        StartHeartbeatSender();
    }

    public async Task Stop()
    {
        await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
    }

    private async Task Connect(string endpoint)
    {
        await _webSocket.ConnectAsync(new Uri(endpoint), CancellationToken.None);
    }

    private void StartHeartbeatSender()
    {
        var timer = new Timer
        {
            Interval = 500
        };
        timer.Elapsed += async (_, _) => await SendHeartbeat();
        timer.Start();
    }

    private async Task SendHeartbeat()
    {
        var heartbeat = new Heartbeat
        {
            Name = "OG2-DV2",
            Type = "Heartbeat",
            Room = 102,
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