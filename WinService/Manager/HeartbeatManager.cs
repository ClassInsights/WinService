using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using WinService.Models;

namespace WinService.Manager;

public class HeartbeatManager
{
    private readonly WinService _winService;

    public HeartbeatManager(WinService winService)
    {
        _winService = winService;
    }

    public async Task Start(CancellationToken token)
    {
        await SendHeartbeat(token);
        var timer = new System.Timers.Timer
        {
            Interval = new Random().Next(20, 60) * 1000,
        };
        timer.Elapsed += async (_, _) => await SendHeartbeat(token);
        timer.Start();
    }

    private async Task SendHeartbeat(CancellationToken token)
    {
        try
        {
            if (!token.IsCancellationRequested && _winService is { Computer: not null, Room: not null })
            {
                _winService.Computer = await _winService.Api.UpdateComputer(new ApiModels.Computer
                (
                    ComputerId: _winService.Computer.ComputerId,
                    LastSeen: DateTime.Now,
#if DEBUG
                    Name: "OG2-DV2",
#else
                    Name: Environment.MachineName,
#endif
                    RoomId: _winService.Room.RoomId,
                    MacAddress: GetMacAddress(),
                    IpAddress: GetLocalIpAddress()
                ));
            }
        }
        catch (Exception e)
        {
            Logger.Error($"Heartbeat failed with error: {e.Message}");
            if (e.InnerException != null) Logger.Error($"InnerException: {e.InnerException.Message}");
        }
    }

    // https://stackoverflow.com/a/6803109/16871250
    private static string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    // https://stackoverflow.com/a/7661829/16871250
    private static string GetMacAddress()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .FirstOrDefault() ?? string.Empty;
    }
}