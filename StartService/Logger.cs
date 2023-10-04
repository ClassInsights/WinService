using System.Reflection;
using System.Runtime.InteropServices;

namespace StartService;

public class Logger
{
    public enum Priority
    {
        Info,
        Debug,
        Warning,
        Error
    }

    public static void Warning(string message)
    {
        Log(message, Priority.Warning);
    }

    public static void Error(string message)
    {
        Log(message, Priority.Error);
    }

    public static void Debug(string message)
    {
        Log(message, Priority.Debug);
    }

    public static void Log(string message, Priority priority = Priority.Info)
    {
        if (!Environment.UserInteractive && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            try
            {
                var dic = Directory.CreateDirectory(
                    $"{Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)}/logs");
                File.AppendAllText($"{dic.FullName}/{DateTime.Now:dd.MM.yy}.txt",
                    $"{DateTime.Now:t} [{$"{priority}]",-10}{message}\n");
            }
            catch (Exception)
            {
                // ignored
            }
        else
            Console.WriteLine(message);
    }
}