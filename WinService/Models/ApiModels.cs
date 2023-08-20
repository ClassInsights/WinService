namespace WinService.Models;

public class ApiModels
{
    public record Class(int ClassId, string Name, string Head, string? AzureGroupID);

    public record Lesson(int LessonId, int RoomId, int SubjectId, int ClassId, DateTime StartTime, DateTime EndTime);

    public record Computer(int ComputerId, int RoomId, string Name, string MacAddress, string IpAddress, DateTime LastSeen);

    public record Room(int RoomId, string Name, string LongName);
}