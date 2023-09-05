﻿using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Configuration;

namespace StartService;

internal static class StartService
{
    public static async Task RunAsync(CancellationToken token)
    {
        try
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            if (config["BaseUrl"] is not { } baseUrl || config["CertificateSubject"] is not { } certSubject)
                throw new Exception("'BaseUrl' or 'CertificateSubject' missing in config!");
            
            var api = new Api(baseUrl, certSubject);
            await api.Authorize();
            
            while (!token.IsCancellationRequested)
            {
                // retrieve lessons
                var lessons = await api.GetLessonsAsync();

                // if no lessons, recheck every hour
                if (lessons is not { Count: not 0 })
                {
                    await Task.Delay(3600000, token);
                    continue;
                }

                // get all start times
                var startTimes = lessons.Select(x => x.StartTime.TimeOfDay).Distinct().ToList();
                
                // wait for next lesson start, but at least 2 minutes
                var startTime = GetNearestTime(startTimes);
                await Task.Delay((int) Math.Max(startTime.TotalMilliseconds - 5000, 120000), token);

                var comingLessons = lessons.Where(x => x.StartTime > DateTime.Now.AddMinutes(-5) && x.StartTime < DateTime.Now.AddMinutes(5)).Select(x => x.RoomId).Distinct().ToList();

                foreach (var roomId in comingLessons)
                {
                    var computers = await api.GetComputersAsync(roomId);
                    if (computers is null)
                        continue;

                    foreach (var computer in computers)
                        if (PhysicalAddress.TryParse(computer.MacAddress, out var address))
                            await address.SendWolAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
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