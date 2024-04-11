using System.Diagnostics;
using System.Management;
using WinService.Models;

namespace WinService.Manager;

public class ShutdownManager(WinService winService)
{
    private const int BufferMinutes = 20; // time until no lessons should be to shutdown

    private const int
        NoLessonsUseTime =
            50; // time how long pc should be usable after all lessons and max delay when recheck for lesson should be

    private List<ApiModels.Lesson> _lessons = [];


    // endless function, will freeze
    public async Task Start(CancellationToken token)
    {
        _lessons = await winService.Api.GetLessonsAsync(winService.Room.RoomId);
        await CheckShutdownLoop(token);
    }

    /// <summary>
    ///     Recheck every 10 minutes and shutdown if no user is logged in
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task CheckLifeSign(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            // wait 10 minutes
            await Task.Delay(600000, token);
            if (GetLoggedInUsername() is null)
                await SendShutdownAsync(token);
        }
    }

    /// <summary>
    ///     Checks if shutdown is ready and sends shutdown to client
    /// </summary>
    /// <param name="token"></param>
    private async Task CheckShutdownLoop(CancellationToken token)
    {
        await Task.Delay(60000, token); // wait 1 Minute for User to sign in and so on ...

        var loopStart = DateTime.Now;
        while (!token.IsCancellationRequested)
            try
            {
                // wait until current lesson is over
                var lessonEnd = GetNextLessonInfo()["endTime"];
                Logger.Log($"Current lesson ends in {lessonEnd} ms!");
                await Task.Delay(lessonEnd, token);
                
                // fetch lessons again, if older than 5 minutes
                if (lessonEnd > 300000) _lessons = await winService.Api.GetLessonsAsync(winService.Room.RoomId);

                // get lesson infos (startTime, endTime) again after lesson is over
                var delay = GetNextLessonInfo();

                // if lessonStart takes longer than buffer or lessonStart is in past, send shutdown
                if (delay["startTime"] / 60000 > BufferMinutes || delay["startTime"] <= 0)
                {
                    // if pc isn't awake at least 5 minutes, then always wait at least NoLessonsUseTime before shutdown
                    if ((DateTime.Now - loopStart).TotalMinutes < 5)
                    {
                        await Task.Delay(NoLessonsUseTime * 60000, token);
                        continue; // check again if any lesson is near
                    }
                    Logger.Log($"Send shutdown at {DateTime.Now.TimeOfDay:g}! startTime: {delay["startTime"]}ms");
                    await SendShutdownAsync(token);
                    loopStart = DateTime.Now; // reset loopStart after shutdown sent that if users aborts after all lessons it'll wait again for NoLessonsUseTime
                    continue; // skip waiting for next lesson, otherwise service could wait long hours if user aborts shutdown (service will now wait for lessonEnd OR NoLessonsUseTime)
                }

                var lessonStart = Math.Min(delay["startTime"], NoLessonsUseTime * 60000);

                // wait until next lesson starts (max duration is NoLessonUseTime)
                // add 5 seconds buffer to be sure current lesson is over and next lessonStart won't be 0 
                await Task.Delay(lessonStart + 5000, token);
            }
            catch (Exception e)
            {
                Logger.Error($"Unhandled {e.GetType()}, Message: {e.Message}, Stacktrace : {e.StackTrace}");
            }
    }


    /// <summary>
    ///     Get information about start and end time of
    ///     <value>_lessons</value>
    /// </summary>
    /// <returns>
    ///     <br>A Dictionary where
    ///         <value>endTime</value>
    ///         contains the duration until the current lesson ends and where
    ///         <value>startTime</value>
    ///         contains the duration until next lesson starts.
    ///     </br>
    ///     <br>Note that
    ///         <value>endTime</value>
    ///         will be set
    ///         <value>NoLessonsUseTime</value>
    ///         minutes if all lessons are over
    ///     </br>
    ///     <br>endTime has a maximum of
    ///         <value>NoLessonsUseTime</value>
    ///     </br>
    /// </returns>
    private Dictionary<string, int> GetNextLessonInfo()
    {
        var endTimes = _lessons.Select(x => x.EndTime.TimeOfDay).Distinct().ToList();
        var startTimes = _lessons.Select(x => x.StartTime.TimeOfDay).Distinct().ToList();

        /*
         get closest times from before 3 minutes, otherwise double lessons without any break will be skipped example:
         
         [lesson 1] endTime 8:35
         [lesson 2] startTime 8:35
         [lesson 3] startTime 9:30
         
         service will wait for end of lesson 1 but if it uses DateTime.Now (which would be 8:35) as default it will think that lesson 2 is already over
         */
        var closestStartTime = GetNearestTime(startTimes, DateTime.Now.AddMinutes(-3).TimeOfDay);
        var closestEndTime = GetNearestTime(endTimes, DateTime.Now.AddMinutes(-3).TimeOfDay);

        Logger.Log("StartTimes: " + string.Join(", ", startTimes.Select(x => x.ToString("g"))));
        Logger.Log("Closest startTime " + closestStartTime.ToString("g"));
        return new Dictionary<string, int>
        {
            ["startTime"] = (int)closestStartTime.TotalMilliseconds,
            ["endTime"] =
                (int)Math.Clamp(closestEndTime.TotalMilliseconds, 60000,
                    NoLessonsUseTime *
                    60000) // wait for a maximum of NoLessonsUseTime for recheck (prevent infinity waiting after user aborts shutdown)
        };
    }


    // https://stackoverflow.com/a/1757221
    /// <summary>
    ///     Get nearest time of list.
    /// </summary>
    /// <param name="times">IEnumerable from which the nearest time should be returned</param>
    /// <param name="date">TimeSpan from where the nearest time should be calculated</param>
    /// <returns>
    ///     Returns TimeSpan which represents the time until the nearest time in IEnumerable
    ///     <value>times</value>
    /// </returns>
    private static TimeSpan GetNearestTime(IEnumerable<TimeSpan> times, TimeSpan? date = null)
    {
        var d = date ?? DateTime.Now.TimeOfDay;

        var timesFuture = times.Where(t => (t - d).TotalMilliseconds > 0).ToList();
        if (!timesFuture.Any()) return TimeSpan.Zero;

        var closestTime = timesFuture.MinBy(t => Math.Abs((t - d).Ticks));
        return closestTime - d;
    }

    /// <summary>
    ///     Sends shutdown to client via pipe
    ///     If no user is logged in it shuts down the pc immediately
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private static async Task SendShutdownAsync(CancellationToken token)
    {
        Logger.Log("Send shutdown to client!");
        var username = GetLoggedInUsername();

        if (username != null)
        {
            try
            {
                if (username.Contains('\\')) username = username.Split("\\")[1];
                await PipeClient.SendCommand( $"ClassInsights-{username}", "shutdown", token);
                return;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to send Shutdown via pipes: {e.Message}");
            }
        } else Process.Start("shutdown", "/s /f /t 60");
    }

    // https://stackoverflow.com/a/7186755
    public static string? GetLoggedInUsername()
    {
        var searcher = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem");
        return searcher.Get().Cast<ManagementBaseObject>().First()["UserName"] as string;
    }
}