using WinService.Models;
using System.Management;
using System.Diagnostics;

namespace WinService;

public class AutoShutdown
{
    private readonly Api _api;
    private List<DbModels.TabLessons> _lessons = new ();
    private DbModels.TabRooms _room = new();
    private const int BufferMinutes = 10;
    private const int DelayNoLessons = 50;

    public AutoShutdown()
    {
        _api = new Api();
    }

    public async Task RunAsync(CancellationToken token)
    {
        /*var pc = Environment.MachineName;
        _room = await _api.GetRoomAsync("DV2");
        StartHeartbeatTimer(token);

        _lessons = await _api.GetLessonsAsync(_room.Id);
        await CheckShutdownLoopAsync(token);*/
        await SendShutdownAsync(token);
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
                var delay = CheckNextShutdown();

                if (delay.TotalMinutes > BufferMinutes) await Task.Delay(delay, token);
                else await SendShutdownAsync(token);
            }
        }, token);
    }

    private TimeSpan CheckNextShutdown()
    {
        if (_lessons.Count == 0) return TimeSpan.FromMinutes(DelayNoLessons);

        var lessonsTimes = _lessons.Select(x => x.EndTime.TimeOfDay).ToList();
        var closestTime = lessonsTimes.MinBy(t => Math.Abs((t - DateTime.Now.TimeOfDay).Ticks));

        if (DateTime.Now.TimeOfDay > closestTime) closestTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromMinutes(DelayNoLessons));

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
}