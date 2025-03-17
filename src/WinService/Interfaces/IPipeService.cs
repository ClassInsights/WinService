using System.Collections.Concurrent;
using WinService.Models;

namespace WinService.Interfaces;

public interface IPipeService
{
    ConcurrentDictionary<string, (StreamWriter Writer, DateTime LastHeartbeat)> Clients { get; }
    Task NotifyClients(PipeModels.IPacket packet);
    string? GetLastUser();
}