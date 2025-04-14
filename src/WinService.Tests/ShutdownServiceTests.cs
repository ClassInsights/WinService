using Microsoft.Extensions.Logging;
using Moq;
using NodaTime;
using NodaTime.Testing;
using WinService.Interfaces;
using WinService.Models;
using WinService.Services;

namespace WinService.Tests;

public class ShutdownServiceTests
{
    [Fact]
    public async Task GetDurationUntilBreakAsync_NoBreak_ReturnDurationUntilEndOfDay()
    {
        var initialInstant = Instant.FromUtc(2025, 1, 29, 8, 0);
        var apiManager = new Mock<IApiManager>();
        
        apiManager.Setup(mock => mock.GetLessonsAsync(null)).ReturnsAsync([
            new ApiModels.Lesson
            {
                Start = Instant.FromUtc(2025, 1, 29, 8, 0),
                End = Instant.FromUtc(2025, 1, 29, 9, 0)
            },
            new ApiModels.Lesson
            {
                Start = Instant.FromUtc(2025, 1, 29, 9, 10),
                End = Instant.FromUtc(2025, 1, 29, 10, 0)
            },
            new ApiModels.Lesson
            {
                Start = Instant.FromUtc(2025, 1, 29, 10, 15), 
                End = Instant.FromUtc(2025, 1, 29, 11, 0)
            },
            new ApiModels.Lesson
            {
                Start = Instant.FromUtc(2025, 1, 29, 11, 10),
                End = Instant.FromUtc(2025, 1, 29, 12, 00)
            }
        ]);
        
        apiManager.Setup(mock => mock.GetSettingsAsync()).ReturnsAsync(new ApiModels.Settings
        {
            NoLessonsTime = 50,
            DelayShutdown = false,
            LessonGapMinutes = 20,
            CheckUser = false,
            CheckGap = true
        });
        
        var shutdownService = new ShutdownService(new Mock<ILogger<ShutdownService>>().Object, new FakeClock(initialInstant), apiManager.Object, new Mock<IPipeService>().Object);
        await shutdownService.LoadSettingsAsync();
        
        var duration = await shutdownService.GetDurationUntilBreakAsync();
        Assert.Equal(Duration.FromHours(4), duration);
    }
    
    [Fact]
    public async Task GetDurationUntilBreakAsync_WithBreak_ReturnDurationUntilStartOfBreak()
    {
        var initialInstant = Instant.FromUtc(2025, 1, 29, 8, 30);
        var apiManager = new Mock<IApiManager>();
        
        apiManager.Setup(mock => mock.GetLessonsAsync(null)).ReturnsAsync([
            new ApiModels.Lesson
            {
                Start = Instant.FromUtc(2025, 1, 29, 8, 0),
                End = Instant.FromUtc(2025, 1, 29, 9, 0)
            },
            new ApiModels.Lesson
            {
                Start = Instant.FromUtc(2025, 1, 29, 9, 10),
                End = Instant.FromUtc(2025, 1, 29, 10, 0)
            },
            new ApiModels.Lesson
            {
                Start = Instant.FromUtc(2025, 1, 29, 10, 30), 
                End = Instant.FromUtc(2025, 1, 29, 11, 0)
            },
            new ApiModels.Lesson
            {
                Start = Instant.FromUtc(2025, 1, 29, 11, 10),
                End = Instant.FromUtc(2025, 1, 29, 12, 00)
            }
        ]);
        
        apiManager.Setup(mock => mock.GetSettingsAsync()).ReturnsAsync(new ApiModels.Settings
        {
            NoLessonsTime = 50,
            DelayShutdown = false,
            LessonGapMinutes = 20,
            CheckUser = false,
            CheckGap = true
        });
        
        var shutdownService = new ShutdownService(new Mock<ILogger<ShutdownService>>().Object, new FakeClock(initialInstant), apiManager.Object, new Mock<IPipeService>().Object);
        await shutdownService.LoadSettingsAsync();
        
        var duration = await shutdownService.GetDurationUntilBreakAsync();
        Assert.Equal(Duration.FromMinutes(90), duration);
    }
    
    [Fact]
    public async Task GetDurationUntilBreakAsync_BeforeLessonsStarted_ReturnDurationUntilStartOfBreak()
    {
        var initialInstant = Instant.FromUtc(2025, 1, 29, 4, 0);
        var apiManager = new Mock<IApiManager>();
        
        apiManager.Setup(mock => mock.GetSettingsAsync()).ReturnsAsync(new ApiModels.Settings
        {
            NoLessonsTime = 50,
            DelayShutdown = false,
            LessonGapMinutes = 20,
            CheckUser = false,
            CheckGap = true
        });
        
        apiManager.Setup(mock => mock.GetLessonsAsync(null)).ReturnsAsync([
            new ApiModels.Lesson
            {
                Start = Instant.FromUtc(2025, 1, 29, 8, 0),
                End = Instant.FromUtc(2025, 1, 29, 9, 0)
            }
        ]);
        
        var shutdownService = new ShutdownService(new Mock<ILogger<ShutdownService>>().Object, new FakeClock(initialInstant), apiManager.Object, new Mock<IPipeService>().Object);
        await shutdownService.LoadSettingsAsync();
        
        var duration = await shutdownService.GetDurationUntilBreakAsync();
        Assert.Equal(Duration.Zero, duration);
    }
}