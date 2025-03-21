using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WinService.Interfaces;
using WinService.Models;

namespace WinService.Services;

public class PipeService(ILogger<PipeService> logger, IApiManager apiManager): BackgroundService, IPipeService
{
     // Concurrent dictionary to store active clients
    public ConcurrentDictionary<string, (StreamWriter Writer, DateTime LastHeartbeat)> Clients { get; } = new();
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _timeoutInterval = TimeSpan.FromSeconds(30);

    private ApiModels.Settings? _settings;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _settings = await apiManager.GetSettingsAsync();
        
        // Start heartbeat monitoring in a separate task
        _ = Task.Run(() => HeartbeatMonitor(stoppingToken), stoppingToken);

        while (!stoppingToken.IsCancellationRequested) // Continuously accept connections
        {
            var server = new NamedPipeServerStream(
                "ClassInsights",
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous
            );

            await server.WaitForConnectionAsync(stoppingToken); // Wait for a new client
            server.ReadMode = PipeTransmissionMode.Message;
            
            logger.LogInformation("Client {Name} connected", server.GetImpersonationUserName());
            // Handle each client in a separate task
            _ = HandleClientAsync(server);
        }
    }
    
    private async Task HandleClientAsync(NamedPipeServerStream server)
    {
        var userName = string.Empty; // Track the client's username
        try
        {
            using var reader = new StreamReader(server);
            var writer = new StreamWriter(server);
            writer.AutoFlush = true;

            // Read client username
            userName = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(userName))
            {
                logger.LogWarning("No user name provided by WinClient");
                await server.DisposeAsync();
                return;
            }
            
            logger.LogInformation("Client connected: {userName}", userName);

            if (_settings?.CheckAfk is true)
            {
                // enable AFK detection
                _ = Task.Run(() => writer.WriteLineAsync(JsonSerializer.Serialize(new PipeModels.Packet<PipeModels.AfkData>
                {
                    Data = new PipeModels.AfkData
                    {
                        Timeout = _settings.AfkTimeout
                    }
                }, SourceGenerationContext.Default.IPacket)));
            }

            // Add the client to the dictionary
            Clients[userName] = (writer, DateTime.UtcNow);

            // Continuously listen for incoming messages
            while (server.IsConnected)
            {
                var message = await reader.ReadLineAsync();
                if (message == null) break; // Exit loop if disconnected

                if (message == "HEARTBEAT")
                {
                    // Update heartbeat timestamp
                    Clients[userName] = (writer, DateTime.UtcNow);
                }
                else
                {
                    logger.LogInformation("Message from {userName}: {message}", userName, message);
                }
            }
        }
        catch (IOException e) // Handle client disconnect
        {
            logger.LogError(e, "Client {userName} disconnected unexpectedly.", userName);
        }
        finally
        {
            if (!string.IsNullOrEmpty(userName))
            {
                // Remove the client on disconnect
                if( Clients.TryRemove(userName, out var client))
                    await client.Writer.DisposeAsync();
                logger.LogInformation("Client {userName} removed from active clients.", userName);
            }

            // Ensure the pipe is closed
            await server.DisposeAsync();
        }
    }

    public async Task NotifyClients(PipeModels.IPacket packet)
    {
        foreach (var client in Clients)
        {
            try
            {
                await client.Value.Writer.WriteLineAsync(JsonSerializer.Serialize(packet, SourceGenerationContext.Default.IPacket));
            }
            catch (IOException e)
            {
                logger.LogError(e, "Failed to send message to {userName}. Removing client.", client.Key);
                Clients.TryRemove(client.Key, out _); // Remove disconnected client
            }
        }
    }
    
    private async Task HeartbeatMonitor(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            foreach (var client in Clients)
            {
                if (now - client.Value.LastHeartbeat <= _timeoutInterval) continue;
                logger.LogInformation("Client {user} timed out. Removing from active clients.", client.Key);
                Clients.TryRemove(client.Key, out _);
            }

            await Task.Delay(_heartbeatInterval, cancellationToken);
        }
    }
    
    public string? GetLastUser()
    {
        return Clients.LastOrDefault().Key;
    }
}