using System.Net;
using System.Net.NetworkInformation;

namespace StartService;

public class StartService
{
    public async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            // retrieve lessons
            var lessons = await Api.GetLessonsAsync();
            
            // get all start times
            var startTimes = lessons.Select(x => x.StartTime.TimeOfDay).Distinct().ToList();
            
            // wait for next lesson start, but at least 2 minutes
            var startTime = GetNearestTime(startTimes);
            await Task.Delay((int) Math.Max(startTime.TotalMilliseconds - 5000, 120000), token);

            var comingLessons = lessons.Where(x => x.StartTime > DateTime.Now.AddMinutes(-5) && x.StartTime < DateTime.Now.AddMinutes(5)).Select(x => x.Room).Distinct().ToList();

            foreach (var roomId in comingLessons)
            {
                var computers = await Api.GetComputersAsync(roomId);
                foreach (var computer in computers.Where(computer => computer.Mac != null))
                {
                    if (PhysicalAddress.TryParse(computer.Mac, out var address))
                        await address.SendWolAsync();
                }
            }
        }
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
}