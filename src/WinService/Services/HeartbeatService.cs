using System.DirectoryServices;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using NodaTime;
using WinService.Interfaces;
using WinService.Models;
using System.DirectoryServices.AccountManagement;

namespace WinService.Services;

public class HeartbeatService(ILogger<HeartbeatService> logger, IClock clock, IApiManager apiManager, IPipeService pipeService): BackgroundService
{
    private readonly PeriodicTimer _timer = new (TimeSpan.FromSeconds(new Random().Next(20, 60)));
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            await SendHeartbeat(stoppingToken);
        } while (!stoppingToken.IsCancellationRequested && await _timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SendHeartbeat(CancellationToken token)
    {
        try
        {
            if (!token.IsCancellationRequested)
            {
                var computer = await apiManager.UpdateComputer(new ApiModels.Computer 
                {
                    ComputerId = apiManager.Computer?.ComputerId ?? 0,
                    LastSeen = clock.GetCurrentInstant(),
                    Name = Environment.MachineName,
                    RoomId = apiManager.Room?.RoomId,
                    MacAddress = GetMacAddress(),
                    IpAddress = GetLocalIpAddress(),
                    LastUser = pipeService.GetLastUser() ?? WindowsIdentity.GetCurrent().Name,
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    OrganizationUnit = GetComputerOu()
                });

                if (computer == null)
                {
                    logger.LogError("Failed to update Computer with id {computerId}", apiManager.Computer?.ComputerId);
                    return;
                }

                apiManager.Computer = computer;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Heartbeat failed with error: {Message}", e.Message);
        }
    }

    private string? GetComputerOu()
    {
        try
        {
            using var context = new PrincipalContext(ContextType.Domain);
            using var computer = ComputerPrincipal.FindByIdentity(context, Environment.MachineName);
            if (computer?.GetUnderlyingObject() is not DirectoryEntry de ||
                de.Properties["distinguishedName"].Count <= 0) return null;
            
            var dn = de.Properties["distinguishedName"][0]?.ToString();
            var ouString = dn?[(dn.IndexOf(',') + 1)..];
            return ouString;
        }
        catch (Exception e)
        {
            logger.LogError("Failed to fetch OU of computer: {message}", e.Message);
            return null;
        }
    }

    // https://stackoverflow.com/a/6803109/16871250
    private string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        logger.LogError("No network adapters with an IPv4 address in the system!");
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