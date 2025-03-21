using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using WinService.Interfaces;
using WinService.Models;

namespace WinService.Services;

public class ShutdownService(ILogger<ShutdownService> logger, IClock clock, IApiManager apiManager, IPipeService pipeService) : BackgroundService
{
    private readonly PeriodicTimer _timer = new (TimeSpan.FromMinutes(7));
    private ApiModels.Settings? _settings;
    private int _counter;
    
    private List<ApiModels.Lesson> _lessons = [];
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await LoadSettingsAsync();
            await Task.WhenAll(CheckLifeSign(stoppingToken), StartShutdownLoop(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    public async Task LoadSettingsAsync() => _settings = await apiManager.GetSettingsAsync() ?? throw new ApplicationException("Failed to load settings");

    private async Task CheckLifeSign(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && _settings!.CheckUser && await _timer.WaitForNextTickAsync(stoppingToken))
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
            await SendShutdownAsync(PipeModels.Reasons.NoUser);
        }
    }

    private async Task StartShutdownLoop(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        while (await WaitUntilShutdownAsync(stoppingToken))
        {
            if (_settings!.DelayShutdown)
                await Task.Delay(TimeSpan.FromMinutes(_settings.ShutdownDelay), stoppingToken);
            
            await SendShutdownAsync(PipeModels.Reasons.LessonsOver, true);
        }
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
            logger.LogInformation("Duration until end of near lessons: {duration}", lessonEndDuration.ToString());
            
            // if all lessons are over, wait for NoLessonsUseTime
            if (lessonEndDuration == Duration.Zero)
            {
                logger.LogInformation("All lessons are over, wait for NoLessonsUseTime");
                await Task.Delay(TimeSpan.FromMinutes(_settings!.NoLessonsTime), stoppingToken);
            }
            else
                await Task.Delay(Duration.Min(updateLessonsInterval, lessonEndDuration).ToTimeSpan(), stoppingToken);
            
            // if we only waited for the interval continue and update lessons
            if (lessonEndDuration >= updateLessonsInterval)
                continue;
            
            logger.LogInformation("Break is here send shutdown");
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
    /// <returns>The duration until the first big break or until all lessons have finished.</returns>
    private Duration GetTimeUntilBreakFromNow()
    {
        // Sort lessons by StartTime (and then by EndTime for safety).
        var sortedLessons = _lessons
            .OrderBy(lesson => lesson.Start)
            .ThenBy(lesson => lesson.End)
            .ToList();

        // Define a 20-minute gap.
        var bigBreakThreshold = Duration.FromMinutes(_settings!.LessonGapMinutes);

        // Get the current instant.
        var now = clock.GetCurrentInstant();

        // If there are no lessons or all lessons have already finished, return zero.
        if (sortedLessons.Count == 0 || sortedLessons.Last().End <= now)
        {
            return Duration.Zero;
        }

        // If now is before the first lesson's start check if we are already in a break
        if (now < sortedLessons.First().Start)
        {
            var gapBeforeFirstLesson = sortedLessons.First().Start - now;
            if (gapBeforeFirstLesson >= bigBreakThreshold)
                return Duration.Zero;
        }
        
        // Iterate over consecutive lessons.
        for (var i = 0; i < sortedLessons.Count - 1; i++)
        {
            var currentLesson = sortedLessons[i];
            var nextLesson = sortedLessons[i + 1];

            // Skip pairs where both lessons are already in the past.
            if (currentLesson.End <= now && nextLesson.Start <= now)
            {
                continue;
            }

            // Define the "effective" end time of the current lesson.
            // If now is after the lesson’s scheduled end, use now (we’re in a gap).
            var effectiveCurrentEnd = currentLesson.End <= now ? now : currentLesson.End;

            // Calculate the gap between the effective end of the current lesson and the start of the next lesson.
            var gap = nextLesson.Start - effectiveCurrentEnd;

            if (gap < bigBreakThreshold) continue;
            
            // If now is already in the gap (i.e. break is ongoing), return zero.
            if (now >= currentLesson.End && now < nextLesson.Start)
            {
                return Duration.Zero;
            }

            // The break begins when the current lesson ends.
            return effectiveCurrentEnd - now;
        }

        // If no big break was found, return the remaining time until the end of the last lesson.
        return sortedLessons.Last().End - now;
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
    private Instant? GetNextLessonStart()
    {
        var startTimes = _lessons.Select(x => x.Start).Distinct().ToList();
        
        var now = clock.GetCurrentInstant();
        var closestStartTime = GetNearestFutureInstant(startTimes, now.Minus(Duration.FromMinutes(3)));
        
        return closestStartTime;
    }
    
    private Instant? GetNearestFutureInstant(List<Instant> instants, Instant? target = null)
    {
        target ??= clock.GetCurrentInstant();

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
    private async Task SendShutdownAsync(PipeModels.Reasons reason, bool lessonCheck = false)
    {
        if (lessonCheck)
        {
            logger.LogInformation("Check again if there are really no lessons near.");
            var duration = await GetDurationUntilBreakAsync();
            
            if (duration != Duration.Zero)
            {
                logger.LogWarning("There are lessons near!");
                return;
            }
            
            logger.LogInformation("No lessons near, send shutdown to user!");
        }
        else
            logger.LogInformation("Send shutdown to user!");

        if (!pipeService.Clients.IsEmpty)
        {
            try
            {
                var now = clock.GetCurrentInstant();
                var lessonStart = GetNextLessonStart();

                var packet = new PipeModels.Packet<PipeModels.ShutdownData>
                {
                    Data = new PipeModels.ShutdownData
                    {
                        Reason = reason,
                        NextLesson = lessonStart > now ? $"{lessonStart:HH:mm}" : null
                    }
                };
                
                await pipeService.NotifyClients(packet);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to send Shutdown via pipes: {Message}", e.Message);
            }
        } else Process.Start("shutdown", "/s /t 60 /c \"Fehlfunktion! Der Computer wird in 60 Sekunden heruntergefahren. Bitte speichern Sie alle wichtigen Daten! Der Shutdown kann mit dem Befehl 'shutdown /a' abgebrochen werden. Diese Nachricht sollten Sie nicht sehen, bitte melden Sie dies der IT-Abteilung! ~ ClassInsights\"");
    }
}