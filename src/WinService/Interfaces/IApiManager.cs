using WinService.Models;

namespace WinService.Interfaces;

public interface IApiManager
{
    Task<List<ApiModels.Lesson>> GetLessonsAsync(int? room = null);
}