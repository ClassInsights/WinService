using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.IO.Pipes;
using Microsoft.Extensions.Logging;

namespace WinService.Services;

public class PipeService(ILogger<PipeService> logger): BackgroundService
{
     // Concurrent dictionary to store active clients
    public readonly ConcurrentDictionary<string, (StreamWriter Writer, DateTime LastHeartbeat)> Clients = new();
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _timeoutInterval = TimeSpan.FromSeconds(30);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
            await using var writer = new StreamWriter(server);
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
                Clients.TryRemove(userName, out _);
                logger.LogInformation("Client {userName} removed from active clients.", userName);
            }

            // Ensure the pipe is closed
            await server.DisposeAsync();
        }
    }

    public async Task NotifyClients(string message)
    {
        foreach (var client in Clients)
        {
            try
            {
                await client.Value.Writer.WriteLineAsync(message);
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