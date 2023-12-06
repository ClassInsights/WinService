using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;
using WinService.Models;
using Timer = System.Timers.Timer;

namespace WinService.Manager;

public class HeartbeatManager(WinService winService)
{
    public async Task Start(CancellationToken token)
    {
        await SendHeartbeat(token);
        var timer = new Timer
        {
            Interval = new Random().Next(20, 60) * 1000
        };
        timer.Elapsed += async (_, _) => await SendHeartbeat(token);
        timer.Start();
    }

    private async Task SendHeartbeat(CancellationToken token)
    {
        try
        {
            if (!token.IsCancellationRequested && winService is { Computer: not null, Room: not null })
                winService.Computer = await winService.Api.UpdateComputer(new ApiModels.Computer
                (
                    winService.Computer.ComputerId,
                    LastSeen: DateTime.Now,
#if DEBUG
                    Name: "OG2-DV2",
#else
                    Name: Environment.MachineName,
#endif
                    RoomId: winService.Room.RoomId,
                    MacAddress: GetMacAddress(),
                    IpAddress: GetLocalIpAddress(),
                    LastUser: ShutdownManager.GetLoggedInUsername() ?? WindowsIdentity.GetCurrent().Name,
                    Version: WinService.Version
                ));
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
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        Logger.Error("No network adapters with an IPv4 address in the system!");
        return string.Empty;
    }

    // https://stackoverflow.com/a/7661829/16871250
    private static string GetMacAddress()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic =>
                nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .FirstOrDefault() ?? string.Empty;
    }
}