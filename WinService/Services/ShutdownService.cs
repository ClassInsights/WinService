using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using WinService.Manager;
using WinService.Models;

namespace WinService.Services;

public class ShutdownService(ILogger<ShutdownService> logger, IClock clock, ApiManager apiManager, PipeService pipeService) : BackgroundService
{
    private const int BufferMinutes = 20; // time until no lessons should be to shut down
    private const int NoLessonsUseTime = 50; // time how long pc should be usable after all lessons and max delay when recheck for lesson should be

    private readonly PeriodicTimer _timer = new (TimeSpan.FromMinutes(7));
    private int _counter;
    
    private List<ApiModels.Lesson> _lessons = [];
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _lessons = await apiManager.GetLessonsAsync();
            await Task.WhenAll(CheckLifeSign(stoppingToken), CheckShutdownLoop(stoppingToken));
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

    /// <summary>
    ///     Checks if shutdown is ready and sends shutdown to client
    /// </summary>
    private async Task CheckShutdownLoop(CancellationToken token)
    {
        await Task.Delay(60000, token); // wait 1 Minute for User to sign in and so on ...

        var loopStart = clock.GetCurrentInstant();
        while (!token.IsCancellationRequested)
            try
            {
                // wait until current lesson is over
                var lessonEnd = GetNextLessonInfo().endTime;
                logger.LogInformation("Current lesson ends in {lessonEnd} ms!", lessonEnd);
                await Task.Delay(lessonEnd, token);
                
                // fetch lessons again, if older than 5 minutes
                if (lessonEnd > 300000) _lessons = await apiManager.GetLessonsAsync();

                // get lesson start again after lesson is over
                var startTime = GetNextLessonInfo().startTime;

                // if lessonStart takes longer than buffer or lessonStart is in the past, send shutdown
                if (startTime / 60000 > BufferMinutes || startTime <= 0)
                {
                    // if computer isn't awake at least 5 minutes, then always wait at least NoLessonsUseTime before shutdown
                    if ((clock.GetCurrentInstant() - loopStart).TotalMinutes < 5)
                    {
                        await Task.Delay(NoLessonsUseTime * 60000, token);
                        continue; // check again if any lesson is near
                    }
                    logger.LogInformation("Next lessons starts in: {startTime}ms", startTime);
                    await SendShutdownAsync();
                    loopStart = clock.GetCurrentInstant(); // reset loopStart after shutdown sent that if users aborts after all lessons it'll wait again for NoLessonsUseTime
                    continue; // skip waiting for next lesson, otherwise service could wait long hours if user aborts shutdown (service will now wait for lessonEnd OR NoLessonsUseTime)
                }

                var lessonStart = Math.Min(startTime, NoLessonsUseTime * 60000);

                // wait until next lesson starts (max duration is NoLessonUseTime)
                // add 5 seconds buffer to be sure current lesson is over and next lessonStart won't be 0 
                await Task.Delay(lessonStart + 5000, token);
            }
            catch (Exception e)
            {
                logger.LogError(e, "{Message}", e.Message);
            }
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
    private (int startTime, int endTime) GetNextLessonInfo()
    {
        var endTimes = _lessons.Select(x => x.EndTime).Distinct().ToList();
        var startTimes = _lessons.Select(x => x.StartTime).Distinct().ToList();
        
        // target needs to be a little in the past, otherwise double lessons will be skipped
        var target = clock.GetCurrentInstant().Minus(Duration.FromMinutes(3));
        var closestStartTime = GetNearestFutureInstant(startTimes, target);
        var closestEndTime = GetNearestFutureInstant(endTimes, target);

        // wait for a maximum of NoLessonsUseTime for recheck (prevent infinity waiting after user aborts shutdown)
        return ((int) (closestStartTime - target).TotalMilliseconds, (int) Math.Clamp((closestEndTime - target).TotalMilliseconds, 60000, NoLessonsUseTime * 60000));
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
                var (lessonStart, _) = GetNextLessonInfo();
                if (lessonStart > 0)
                {
                    var nextLesson = clock.GetCurrentInstant().Plus(Duration.FromMilliseconds(lessonStart));
                    await pipeService.NotifyClients($"NextLesson_{nextLesson:HH:mm}");
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