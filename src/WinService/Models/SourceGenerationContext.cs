using System.Text.Json.Serialization;

namespace WinService.Models;

[JsonSerializable(typeof(ApiModels.Class))]
[JsonSerializable(typeof(ApiModels.Lesson))]
[JsonSerializable(typeof(List<ApiModels.Lesson>))]
[JsonSerializable(typeof(ApiModels.Computer))]
[JsonSerializable(typeof(ApiModels.Room))]
[JsonSerializable(typeof(ApiModels.Settings))]
[JsonSerializable(typeof(ApiModels.Client))]
[JsonSerializable(typeof(PipeModels.IPacket))]
[JsonSerializable(typeof(PipeModels.Packet<PipeModels.ShutdownData>))]
[JsonSerializable(typeof(PipeModels.Packet<PipeModels.LogOffData>))]
[JsonSerializable(typeof(PipeModels.Packet<PipeModels.AfkData>))]
[JsonSourceGenerationOptions(WriteIndented = false, PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class SourceGenerationContext : JsonSerializerContext;