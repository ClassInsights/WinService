using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using WinService.Interfaces;

namespace WinService.Manager;

public class VersionManager(IApiManager apiManager, ILogger<VersionManager> logger): IVersionManager
{
    public async Task<bool> UpdateAsync()
    {
        if (!await UpdateAvailable())
            return false;
        
        logger.LogInformation("New update available, downloading the installer!");
        var installerBytes = await apiManager.GetClientInstallerAsync();
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        await File.WriteAllBytesAsync(tempFile, installerBytes);
        
        logger.LogInformation("Run the client installer!");
        var installerProcess = new Process();
        installerProcess.StartInfo.FileName = "msiexec";
        installerProcess.StartInfo.Arguments = $"/i \"{tempFile}\" /qn";
        installerProcess.StartInfo.UseShellExecute = false;
        installerProcess.StartInfo.CreateNoWindow = true;
        installerProcess.Start();
        return true;
    }

    private async Task<bool> UpdateAvailable()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        var client = await apiManager.GetClientAsync();
        return version != (client?.ClientVersion ?? version);
    }
}