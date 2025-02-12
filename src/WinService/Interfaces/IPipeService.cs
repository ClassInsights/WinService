using System.Collections.Concurrent;

namespace WinService.Interfaces;

public interface IPipeService
{
    ConcurrentDictionary<string, (StreamWriter Writer, DateTime LastHeartbeat)> Clients { get; }
    Task NotifyClients(string message);
    string? GetLastUser();
}