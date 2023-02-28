using WinService.Models;
using System.Management;
using System.Diagnostics;

namespace WinService;

public class AutoShutdown
{
    private readonly Api _api;
    private List<DbModels.TabLessons> _lessons = new ();
    private DbModels.TabRooms _room = new();
    private const int BufferMinutes = 20; // time where no lessons should be
    private const int NoLessonsUseTime = 50; // time how long pc should be usable after all lessons

    public AutoShutdown()
    {
        _api = new Api();
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
        while (!token.IsCancellationRequested)
        {
            var rnd = new Random();
            await _api.UpdateComputer(new RequestModels.ComputerRequest
            {
                LastSeen = DateTime.Now,
                Name = _room.Name,
                Room = _room.Id
            });
            token.WaitHandle.WaitOne(TimeSpan.FromSeconds(rnd.Next(20, 60)));
        }
    }

    private async Task CheckShutdownLoopAsync(CancellationToken token)
    {
        await Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var delay = await WaitLessonEnd();

                    if (delay["startTime"].TotalMinutes > BufferMinutes) await SendShutdownAsync(token); // if lessons start takes longer than buffer => send shutdown
                    await Task.Delay((int) Math.Max(delay["endTime"].TotalMilliseconds, NoLessonsUseTime), token); // wait until lesson end, if all lessons are over wait for NoLessonsUseTime
                }
                catch (NoLessonsException)
                {
                    _lessons.Add(new DbModels.TabLessons()
                    {
                        StartTime = DateTime.Now,
                        EndTime = DateTime.Now.AddMinutes(1),
                    });
                }
            }
        }, token);
    }

    private async Task<Dictionary<string, TimeSpan>> WaitLessonEnd()
    {
        while (true)
        {
            if (_lessons.Count == 0) throw new NoLessonsException();

            var endTimes = _lessons.Select(x => x.EndTime.TimeOfDay).ToList();
            var startTimes = _lessons.Select(x => x.StartTime.TimeOfDay).ToList();

            var closestEndTime = GetNearestTime(endTimes);
            var closestStartTime = GetNearestTime(startTimes);
            
            if (closestEndTime > closestStartTime) 
                return new Dictionary<string, TimeSpan>
                {
                    ["startTime"] = closestStartTime,
                    ["endTime"] = closestEndTime
                };
            await Task.Delay(closestEndTime); // wait for lessons end, if end is before next lesson start
        }
    }

    private TimeSpan GetNearestTime(IEnumerable<TimeSpan> times)
    {
        var closestTime = times.MinBy(t => Math.Abs((t - DateTime.Now.TimeOfDay).Ticks));
        return closestTime - DateTime.Now.TimeOfDay; 
    }

    private async Task SendShutdownAsync(CancellationToken token)
    {
        var username = GetLoggedInUsername();
        if (username is null) // shutdown pc if no user is logged in
        {
            Process.Start("shutdown", "/s");
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

    public class NoLessonsException : Exception {}
}