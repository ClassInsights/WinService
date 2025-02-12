using WinService.Models;

namespace WinService.Interfaces;

public interface IApiManager
{
    Task<List<ApiModels.Lesson>> GetLessonsAsync(int? room = null);
    Task<ApiModels.Computer?> UpdateComputer(ApiModels.Computer request);
    ApiModels.Computer? Computer { get; set; }
    ApiModels.Room Room { get; }
    string? ApiUrl { get; }
    string? Token { get; }
}