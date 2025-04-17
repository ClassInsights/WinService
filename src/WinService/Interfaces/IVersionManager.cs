namespace WinService.Interfaces;

public interface IVersionManager
{
    Task<bool> UpdateAsync();
}