using System.Text.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace WinService.Models;

public class ApiModels
{
    public class Class
    {
        public int ClassId { get; set; }
        public string Name { get; set; } = null!;
        public string Head { get; set; } = null!;
        public string? AzureGroupId { get; set; }
    }

    public class Lesson
    {
        public int LessonId { get; set; }
        public int RoomId { get; set; }
        public int SubjectId { get; set; }
        public int ClassId { get; set; }
        [JsonConverter(typeof(NodaTimeDefaultJsonConverterFactory))]
        public Instant Start { get; set; }
        [JsonConverter(typeof(NodaTimeDefaultJsonConverterFactory))]
        public Instant End { get; set; }
    }

    public class Computer // yyyy-MM-ddTHH:mm:ss.fffZ
    {
        public long? ComputerId { get; set; }
        public long? RoomId { get; set; }
        public string Name { get; set; } = null!;
        public string MacAddress { get; set; } = null!;
        public string IpAddress { get; set; } = null!;
        [JsonConverter(typeof(NodaTimeDefaultJsonConverterFactory))]
        public Instant LastSeen { get; set; }
        public string LastUser { get; set; } = null!;
        public string? Version { get; set; }
        public string? OrganizationUnit { get; set; }
    }

    public class Room
    {
        public int RoomId { get; set; }
        public string DisplayName { get; set; } = null!;
        public bool Enabled { get; set; }
    }
    
    public class Settings
    {
        public bool CheckGap { get; set; }
        public int LessonGapMinutes { get; set; }
        public int NoLessonsTime { get; set; }
        public bool CheckUser { get; set; }
        public bool CheckAfk { get; set; }
        public int AfkTimeout { get; set; }
        public bool DelayShutdown { get; set; }
        public int ShutdownDelay { get; set; }
    }

    public class Client
    {
        [JsonPropertyName("client_version")]
        public string ClientVersion { get; set; } = null!;
    }
    
    public class ComputerLog
    {
        public long? ComputerId { get; set; }
        [JsonConverter(typeof(NodaTimeDefaultJsonConverterFactory))]
        public Instant Timestamp { get; set; }

        public string Category { get; set; } = null!;
        public string Level { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? Details { get; set; }
    }
}