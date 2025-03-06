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
        public int? ComputerId { get; set; }
        public int RoomId { get; set; }
        public string Name { get; set; } = null!;
        public string MacAddress { get; set; } = null!;
        public string IpAddress { get; set; } = null!;
        [JsonConverter(typeof(NodaTimeDefaultJsonConverterFactory))]
        public Instant LastSeen { get; set; }
        public string LastUser { get; set; } = null!;
        public string? Version { get; set; }
    }

    public class Room
    {
        public int RoomId { get; set; }
        public string Name { get; set; } = null!;
        public string LongName { get; set; } = null!;
    }
    
    public class Settings
    {
        public int LessonGapMinutes { get; set; }
        public int NoLessonsTime { get; set; }
        public bool CheckUser { get; set; }
        public bool CheckAfk { get; set; } // todo: implement AFK check
        public int AfkTimeout { get; set; }
        public bool DelayShutdown { get; set; }
        public int ShutdownDelay { get; set; }
    }
}

[JsonSerializable(typeof(ApiModels.Class))]
[JsonSerializable(typeof(ApiModels.Lesson))]
[JsonSerializable(typeof(List<ApiModels.Lesson>))]
[JsonSerializable(typeof(ApiModels.Computer))]
[JsonSerializable(typeof(ApiModels.Room))]
[JsonSerializable(typeof(ApiModels.Settings))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class SourceGenerationContext : JsonSerializerContext;