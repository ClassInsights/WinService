using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using WinService.Models;

namespace WinService.Manager;

public class ShutdownManager
{
    private readonly WinService _winService;
    private const int BufferMinutes = 20; // time until no lessons should be to shutdown
    private const int NoLessonsUseTime = 50; // time how long pc should be usable after all lessons and max delay when recheck for lesson should be
    private List<DbModels.TabLessons> _lessons = new();


    public ShutdownManager(WinService winService)
    {
        _winService = winService;
    }

    public async Task Start(CancellationToken token)
    {
        _lessons = await Api.GetLessonsAsync(_winService.Room.Id);
        CheckShutdownLoop(token);
    }

    /// <summary>
    /// Checks if shutdown is ready and sends shutdown to client
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private void CheckShutdownLoop(CancellationToken token)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(60000, token); // wait 1 Minute for User to sign in and so on ...

            var loopStart = DateTime.Now;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // wait until current lesson is over
                    var lessonEnd = GetNextLessonInfo()["endTime"];
                    await Task.Delay(lessonEnd, token);

                    // get lesson infos (startTime, endTime) again after lesson is over
                    var delay = GetNextLessonInfo();

                    // if lessonStart takes longer than buffer or lessonStart is in past, send shutdown
                    if (delay["startTime"] / 60000 > BufferMinutes || delay["startTime"] <= 0)
                    {
                        // if pc isn't awake at least 5 minutes, then always wait at least NoLessonsUseTime before shutdown
                        if ((DateTime.Now - loopStart).TotalMinutes < 5)
                            await Task.Delay(NoLessonsUseTime * 60000, token);

                        await SendShutdownAsync(token);
                        loopStart = DateTime.Now; // reset loopStart after shutdown sent, that if users aborts after all lessons it'll wait again for NoLessonsUseTime
                        continue; // skip waiting for next lesson, otherwise service could wait long hours if user aborts shutdown (service will now wait for lessonEnd OR NoLessonsUseTime)
                    }

                    var lessonStart = Math.Min(delay["startTime"], NoLessonsUseTime * 60000);

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

        return new Dictionary<string, int>
        {
            ["startTime"] = (int)closestStartTime.TotalMilliseconds,
            ["endTime"] = (int)Math.Clamp(closestEndTime.TotalMilliseconds, 60000, NoLessonsUseTime * 60000) // wait for a maximum of NoLessonsUseTime for recheck (prevent infinity waiting after user aborts shutdown)
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
    /// If no user is logged in it shuts down the pc immediately
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