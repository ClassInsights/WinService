using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using WinService.Interfaces;
using WinService.Models;

namespace WinService.Services;

public class ShutdownService(ILogger<ShutdownService> logger, IClock clock, IApiManager apiManager, IPipeService pipeService) : BackgroundService
{
    private const int NoLessonsTime = 50; // time how long pc should be usable after all lessons and max delay when recheck for lesson should be

    private readonly PeriodicTimer _timer = new (TimeSpan.FromMinutes(7));
    private int _counter;
    
    private List<ApiModels.Lesson> _lessons = [];
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _lessons = await apiManager.GetLessonsAsync();
            await Task.WhenAll(CheckLifeSign(stoppingToken), StartShutdownLoop(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    private async Task CheckLifeSign(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && await _timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!pipeService.Clients.IsEmpty)
            {
                _counter = 0;
                _timer.Period = TimeSpan.FromMinutes(7);
                continue;
            }

            _counter++;
            _timer.Period = _counter < 3 ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(7);

            if (_counter < 3)
                continue;
            
            _counter = 0;
            await SendShutdownAsync();
        }
    }

    private async Task StartShutdownLoop(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); 
        if (await WaitUntilShutdownAsync(stoppingToken)) await SendShutdownAsync();
    }
    
    /// <summary>
    /// Wait until a shutdown should be sent to the user. The lessons will be updated every hour.
    /// </summary>
    /// <returns></returns>
    private async Task<bool> WaitUntilShutdownAsync(CancellationToken stoppingToken)
    {
        var updateLessonsInterval = Duration.FromHours(1);
        while (!stoppingToken.IsCancellationRequested)
        {
            var lessonEndDuration = await GetDurationUntilBreakAsync();
            
            // if all lessons are over, wait for NoLessonsUseTime
            if (lessonEndDuration == Duration.Zero)
                await Task.Delay(NoLessonsTime, stoppingToken);
            else
                await Task.Delay(Duration.Min(updateLessonsInterval, lessonEndDuration).ToTimeSpan(), stoppingToken);
            
            // if break is here send shutdown
            if (lessonEndDuration < updateLessonsInterval)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Returns the Duration after which no lesson is near
    /// </summary>
    public async Task<Duration> GetDurationUntilBreakAsync()
    {
        _lessons = await apiManager.GetLessonsAsync();
        return GetTimeUntilBreakFromNow();
    }

    /// <summary>
    /// Returns the duration from now until the beginning of the first gap between lessons.
    /// If now is already within a qualifying break, returns Duration.Zero.
    /// If no break exists, returns the duration from now until the end of the last lesson.
    /// </summary>
    /// <param name="gapMinutes">Time from which a break is defined in minutes</param>
    /// <returns>The duration until the first big break or until all lessons have finished.</returns>
    private Duration GetTimeUntilBreakFromNow(int gapMinutes = 20)
    {
        // Sort lessons by StartTime (and then by EndTime for safety).
        var sortedLessons = _lessons
            .OrderBy(lesson => lesson.StartTime)
            .ThenBy(lesson => lesson.EndTime)
            .ToList();

        // Define a 20-minute gap.
        var bigBreakThreshold = Duration.FromMinutes(gapMinutes);

        // Get the current instant.
        var now = clock.GetCurrentInstant();

        // If there are no lessons or all lessons have already finished, return zero.
        if (sortedLessons.Count == 0 || sortedLessons.Last().EndTime <= now)
        {
            return Duration.Zero;
        }

        // Iterate over consecutive lessons.
        for (var i = 0; i < sortedLessons.Count - 1; i++)
        {
            var currentLesson = sortedLessons[i];
            var nextLesson = sortedLessons[i + 1];

            // Skip pairs where both lessons are already in the past.
            if (currentLesson.EndTime <= now && nextLesson.StartTime <= now)
            {
                continue;
            }

            // Define the "effective" end time of the current lesson.
            // If now is after the lesson’s scheduled end, use now (we’re in a gap).
            var effectiveCurrentEnd = currentLesson.EndTime <= now ? now : currentLesson.EndTime;

            // Calculate the gap between the effective end of the current lesson and the start of the next lesson.
            var gap = nextLesson.StartTime - effectiveCurrentEnd;

            if (gap < bigBreakThreshold) continue;
            
            // If now is already in the gap (i.e. break is ongoing), return zero.
            if (now >= currentLesson.EndTime && now < nextLesson.StartTime)
            {
                return Duration.Zero;
            }

            // The break begins when the current lesson ends.
            return effectiveCurrentEnd - now;
        }

        // If no big break was found, return the remaining time until the end of the last lesson.
        return sortedLessons.Last().EndTime - now;
    }
    
    
    /// <summary>
    ///     Get information about start and end time of the next lesson
    /// </summary>
    /// <returns>
    ///  A Tuple where endTime contains the duration until the current lesson ends and where
    ///  startTime contains the duration until next lesson starts in milliseconds.
    /// </returns>
    /// <remarks>
    /// <para>endTime will be set NoLessonsUseTime minutes if all lessons are over</para>
    /// <para>endTime has a maximum of NoLessonsUseTime</para>
    /// </remarks>
    private async Task<(Instant nextLessonStart, Instant nextLessonEnd)> GetNextLessonInfoAsync()
    {
        _lessons = await apiManager.GetLessonsAsync();
        var endTimes = _lessons.Select(x => x.EndTime).Distinct().ToList();
        var startTimes = _lessons.Select(x => x.StartTime).Distinct().ToList();
        
        var now = clock.GetCurrentInstant();
        // target needs to be a little in the past, otherwise double lessons will be skipped
        var target = now.Minus(Duration.FromMinutes(3));
        
        var closestStartTime = GetNearestFutureInstant(startTimes, target);
        var closestEndTime = GetNearestFutureInstant(endTimes, target);
        var noLessonsTime = now.Plus(Duration.FromMinutes(NoLessonsTime));
        
        // wait for a maximum of NoLessonsUseTime for recheck (prevent infinity waiting after user aborts shutdown)
        return (closestStartTime, Instant.Min(noLessonsTime, closestEndTime));
    }
    
    private Instant GetNearestFutureInstant(List<Instant> instants, Instant? target = null)
    {
        target ??= clock.GetCurrentInstant();
        if (instants == null || instants.Count == 0)
            throw new ArgumentException("The list of instants cannot be null or empty.");

        return instants
            .Where(instant => instant > target)
            .OrderBy(instant => instant - target)
            .FirstOrDefault();
    }
    

    /// <summary>
    ///     Sends shutdown to client via pipe
    ///     If no user is logged in it shuts down the pc immediately
    /// </summary>
    /// <returns></returns>
    private async Task SendShutdownAsync()
    {
        logger.LogInformation("Send shutdown to client!");

        if (!pipeService.Clients.IsEmpty)
        {
            try
            {
                var now = clock.GetCurrentInstant();
                var (lessonStart, _) = await GetNextLessonInfoAsync();
                if (lessonStart > now)
                {
                    await pipeService.NotifyClients($"NextLesson_{lessonStart:HH:mm}");
                }
                await pipeService.NotifyClients("shutdown");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to send Shutdown via pipes: {Message}", e.Message);
            }
        } else Process.Start("shutdown", "/s /f /t 60");
    }
}