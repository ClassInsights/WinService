using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WinService.Interfaces;
using WinService.Manager;
using WinService.Models;

namespace WinService.Services;

public class WebSocketService(ILogger<WebSocketService> logger, IApiManager apiManager, IPipeService pipeService): BackgroundService
{
    private readonly EnergyManager _energyManager = new();
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));
    private ClientWebSocket _webSocket = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await RunAsync(stoppingToken);
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Dispose();
        _energyManager.Dispose();
        await CloseWebSocket();
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        var uri = new Uri(apiManager.ApiUrl ?? string.Empty);
        if (uri == null)
            throw new Exception("Invalid API URL");
        
        try
        {
            await ConnectAsync($"ws://{uri.Authority}/ws/computers", stoppingToken); // todo: use wss:// as default
            
            // start listening for commands
            _ = Task.Run(() => ReadCommandsAsync(stoppingToken), stoppingToken);
            
            // send heartbeats
            while (!stoppingToken.IsCancellationRequested && await _timer.WaitForNextTickAsync(stoppingToken)) await SendHeartbeatAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "{Message}", e.Message);
            await Task.Delay(5000, stoppingToken);
            await ReconnectAsync(stoppingToken);
        }
    }

    private async Task ReconnectAsync(CancellationToken token)
    {
        logger.LogInformation("Reconnecting to WebSocket ...");
        await CloseWebSocket();
        _webSocket = new ClientWebSocket();
        await RunAsync(token);
    }
    

    private async Task CloseWebSocket()
    {
        if (_webSocket.State is not WebSocketState.Closed and not WebSocketState.Aborted)
        {
            await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            _webSocket.Abort();
        }
    }

    private async Task ConnectAsync(string endpoint, CancellationToken token = default)
    {
        _webSocket.Options.SetRequestHeader("Authorization", $"Bearer { apiManager.Token }");
        await _webSocket.ConnectAsync(new Uri(endpoint), token);
        logger.LogInformation("Connected to Websocket!");
    }
    
    private async Task ReadCommandsAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var command = await ReadTextAsync(token);
                logger.LogInformation("Received {command} received!", command);
                switch (command)
                {
                    case "shutdown":
                        Process.Start("shutdown", "/s /f /t 0");
                        break;
                    case "restart":
                        Process.Start("shutdown", "/r /f /t 0");
                        break;
                    case "logoff":
                        await pipeService.NotifyClients(new PipeModels.Packet<PipeModels.LogOffData>());
                        break;
                }
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception e)
        {
            logger.LogError(e, "{Message}", e.Message);
            await Task.Delay(5000, token);
            await ReconnectAsync(token);
        }
    }

    private async Task SendHeartbeatAsync()
    {
        if (_webSocket.State != WebSocketState.Open) return;

        try
        {
            _energyManager.UpdateValues();

            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                apiManager.Computer?.ComputerId,
                Name = Environment.MachineName,
                Type = "Heartbeat",
                Room = apiManager.Room.RoomId,
                UpTime = DateTime.Now.AddMilliseconds(-1 * Environment.TickCount64),
                Data = new
                {
                    CpuUsage = _energyManager.GetCpuUsages(),
                    Power = _energyManager.GetPowerUsage(),
                    EthernetUsages = _energyManager.GetEthernetUsages(),
                    RamUsage = _energyManager.GetRamUsage(),
                    DiskUsages = _energyManager.GetDiskUsages()
                }
            })));
            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while sending Heartbeat: {Message}", e.Message);
        }
    }

    private async Task<string?> ReadTextAsync(CancellationToken token = default)
    {
        var buffer = new byte[8192];
        var text = new StringBuilder();

        WebSocketReceiveResult receiveResult;
        do
        {
            receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
            if (receiveResult.MessageType != WebSocketMessageType.Close)
            {
                text.Append(Encoding.UTF8.GetString(new ArraySegment<byte>(buffer, 0, receiveResult.Count)));
            }
            else
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, _webSocket.CloseStatusDescription, token);
                return null; // return null if close
            }
        } while (!receiveResult.EndOfMessage);

        return text.ToString();
    }
}