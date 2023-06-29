using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using WinService.Models;

namespace WinService;

public class AutoShutdown
{
    private readonly Api _api;
    private List<DbModels.TabLessons> _lessons = new ();
    private DbModels.TabRooms _room = new();
    private const int BufferMinutes = 20; // time until no lessons should be to shutdown
    private const int NoLessonsUseTime = 50; // time how long pc should be usable after all lessons and max delay when recheck for lesson should be

    public AutoShutdown()
    {
        _api = new Api();
        Logger.Log("Init AutoShutdown");
    }

    public async Task RunAsync(CancellationToken token)
    {
        var pc = Environment.MachineName;
        _room = await _api.GetRoomAsync("DV2");
        StartHeartbeatTimer(token);

        _lessons = await _api.GetLessonsAsync(_room.Id);

        await CheckShutdownLoopAsync(token);
        await Task.Delay(-1, token);
    }

    private void StartHeartbeatTimer(CancellationToken token)
    {
        var timer = new System.Timers.Timer
        {
            Interval = new Random().Next(20, 60) * 1000,
        };
        timer.Elapsed += (_, _) => SendHeartbeats(token);
        timer.Start();
    }

    private async void SendHeartbeats(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await _api.UpdateComputer(new DbModels.TabComputers
                {
                    LastSeen = DateTime.Now,
                    Name = _room.Name,
                    Room = _room.Id,
                    Mac = GetMacAddress(),
                    Ip = GetLocalIpAddress()
                });
                token.WaitHandle.WaitOne(TimeSpan.FromSeconds(new Random().Next(20, 60)));
            }
        }
        catch (Exception e)
        {
            Logger.Error($"Heartbeat failed with error: {e.Message}");
        }
    }

    // https://stackoverflow.com/a/6803109/16871250
    private static string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    // https://stackoverflow.com/a/7661829/16871250
    private static string GetMacAddress()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .FirstOrDefault() ?? string.Empty;
    }

    /// <summary>
    /// Checks if shutdown is ready and sends shutdown to client
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private async Task CheckShutdownLoopAsync(CancellationToken token)
    {
        await Task.Run(async () =>
        {
            await Task.Delay(60000, token); // wait 1 Minute for User to sign in and so on ...
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // wait until current lesson is over
                    var lessonEnd = GetNextLessonInfo()["endTime"];
                    Logger.Log($"Wait {lessonEnd / 1000}s for lesson end!");
                    await Task.Delay(lessonEnd, token);

                    // get lesson infos (startTime, endTime) again after lesson is over
                    var delay = GetNextLessonInfo();

                    // if lessonStart takes longer than buffer or lessonStart is in past, send shutdown
                    if (delay["startTime"] / 60000 > BufferMinutes || delay["startTime"] <= 0)
                    {
                        await SendShutdownAsync(token);
                        continue; // skip waiting for next lesson, otherwise service could wait long hours if user aborts shutdown (service will now wait for lessonEnd OR NoLessonsUseTime)
                    }

                    var lessonStart = Math.Min(delay["startTime"], NoLessonsUseTime * 60000);
                    Logger.Log($"Wait {lessonStart / 1000}s for next lesson to start!");

                    // wait until next lesson starts (max duration is NoLessonUseTime)
                    await Task.Delay(lessonStart, token);
                }
                catch (Exception e)
                {
                    Logger.Error($"Unhandled {e.GetType()}, Message: {e.Message}, Stacktrace : {e.StackTrace}");
                }
            }
        }, token);
    }

    /// <summary>
    /// Get information about start and end time of <value>_lessons</value>
    /// </summary>
    /// <returns>
    /// <br>A Dictionary where <value>endTime</value> contains the duration until the current lesson ends and where <value>startTime</value> contains the duration until next lesson starts.</br>
    /// <br>Note that <value>endTime</value> will be set <value>NoLessonsUseTime</value> minutes if all lessons are over</br>
    /// <br>endTime has a maximum of <value>NoLessonsUseTime</value></br>
    /// </returns>
    private Dictionary<string, int> GetNextLessonInfo()
    {
        var endTimes = _lessons.Select(x => x.EndTime.TimeOfDay).Distinct().ToList();
        var startTimes = _lessons.Select(x => x.StartTime.TimeOfDay).Distinct().ToList();

        var closestStartTime = GetNearestTime(startTimes);
        var closestEndTime = GetNearestTime(endTimes);

        Logger.Debug($"ClosestStartTime: {closestStartTime.TotalSeconds}s");
        Logger.Debug($"ClosestEndTime: {closestEndTime.TotalSeconds}s");

        return new Dictionary<string, int>
        {
            ["startTime"] = (int) closestStartTime.TotalMilliseconds,
            ["endTime"] = (int) Math.Clamp(closestEndTime.TotalMilliseconds, 60000, NoLessonsUseTime * 60000) // wait for a maximum of NoLessonsUseTime for recheck (prevent infinity waiting after user aborts shutdown)
        };
    }

    // https://stackoverflow.com/a/1757221
    /// <summary>
    /// Get nearest time of list.
    /// </summary>
    /// <param name="times">IEnumerable from which the nearest time should be returned</param>
    /// <param name="date">TimeSpan from where the nearest time should be calculated</param>
    /// <returns>
    /// Returns TimeSpan which represents the time until the nearest time in IEnumerable <value>times</value>
    /// </returns>
    private static TimeSpan GetNearestTime(IEnumerable<TimeSpan> times, TimeSpan? date = null)
    {
        var d = date ?? DateTime.Now.TimeOfDay;

        var timesFuture = times.Where(t => (t - d).TotalMilliseconds > 0).ToList();
        if (!timesFuture.Any()) return TimeSpan.Zero;

        var closestTime = timesFuture.MinBy(t => Math.Abs((t - d).Ticks));
        return closestTime - DateTime.Now.TimeOfDay; 
    }

    /// <summary>
    /// Sends shutdown to client via pipe
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private static async Task SendShutdownAsync(CancellationToken token)
    {
        Logger.Log("Send shutdown to client!");
        var username = GetLoggedInUsername();
        if (username is null) // shutdown pc immediately if no user is logged in
        {
            Process.Start("shutdown", "/s /f /t 0");
            return;
        }

        if (username.Contains('\\')) username = username.Split("\\")[1];

        var pipeName = $"AutoShutdown-{username}";
        await PipeClient.SendShutdown(pipeName, token);
    }

    // https://stackoverflow.com/a/7186755
    private static string? GetLoggedInUsername()
    {
        if (!OperatingSystem.IsWindows()) throw new NotImplementedException();
        var searcher = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem");
        var collection = searcher.Get();
        return collection.Cast<ManagementBaseObject>().First()["UserName"] as string;
    }
}