using WinService.Models;

namespace WinService.Interfaces;

public interface IApiManager
{
    Task<List<ApiModels.Lesson>> GetLessonsAsync(int? room = null);
    Task<ApiModels.Settings?> GetSettingsAsync();
    Task<ApiModels.Computer?> UpdateComputer(ApiModels.Computer request);
    Task<ApiModels.Client?> GetClientAsync();
    Task<byte[]> GetClientInstallerAsync();
    Task BatchLogs(List<ApiModels.ComputerLog> logs);
    ApiModels.Computer? Computer { get; set; }
    ApiModels.Room? Room { get; }
    string? ApiUrl { get; }
    string? Token { get; }
}