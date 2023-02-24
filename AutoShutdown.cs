using WinService.Models;

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
        _room = await _api.GetRoomAsync("DV4");
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
                var delay = CheckNextShutdown();

                if (delay.TotalMinutes > BufferMinutes) await Task.Delay(delay, token);
                else await SendShutdownAsync();
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

    private async Task SendShutdownAsync()
    {
        await Task.Run(() => {});
    }
}