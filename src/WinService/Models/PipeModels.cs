using System.Text.Json.Serialization;

namespace WinService.Models;

public class PipeModels
{
    [JsonDerivedType(typeof(Packet<ShutdownData>))]
    [JsonDerivedType(typeof(Packet<LogOffData>))]
    [JsonDerivedType(typeof(Packet<AfkData>))]
    public interface IPacket
    {
        Type PacketType { get; }
    }

    public class Packet<T>: IPacket
    {
        public Type PacketType
        {
            get
            {
                if (typeof(T) == typeof(ShutdownData))
                    return Type.Shutdown;
                if (typeof(T) == typeof(AfkData))
                    return Type.Afk;
                if (typeof(T) == typeof(LogOffData))
                    return Type.Logoff;
                return Type.None;
            }
        }

        public T Data { get; set; }
    }
    
    public class ShutdownData
    {
        public Reasons Reason { get; set; }
        public string? NextLesson { get; set; }
    }

    public class LogOffData;

    public class AfkData
    {
        public int Timeout { get; set; }
    }

    public enum Reasons
    {
        LessonsOver,
        NoUser
    }
    
    public enum Type
    {
        Shutdown,
        Logoff,
        Afk,
        None
    }
}