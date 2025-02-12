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
                StartTime = Instant.FromUtc(2025, 1, 29, 8, 0),
                EndTime = Instant.FromUtc(2025, 1, 29, 9, 0)
            },
            new ApiModels.Lesson
            {
                StartTime = Instant.FromUtc(2025, 1, 29, 9, 10),
                EndTime = Instant.FromUtc(2025, 1, 29, 10, 0)
            },
            new ApiModels.Lesson
            {
                StartTime = Instant.FromUtc(2025, 1, 29, 10, 15), 
                EndTime = Instant.FromUtc(2025, 1, 29, 11, 0)
            },
            new ApiModels.Lesson
            {
                StartTime = Instant.FromUtc(2025, 1, 29, 11, 10),
                EndTime = Instant.FromUtc(2025, 1, 29, 12, 00)
            }
        ]);
        
        var shutdownService = new ShutdownService(new Mock<ILogger<ShutdownService>>().Object, new FakeClock(initialInstant), apiManager.Object, new Mock<IPipeService>().Object);
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
                StartTime = Instant.FromUtc(2025, 1, 29, 8, 0),
                EndTime = Instant.FromUtc(2025, 1, 29, 9, 0)
            },
            new ApiModels.Lesson
            {
                StartTime = Instant.FromUtc(2025, 1, 29, 9, 10),
                EndTime = Instant.FromUtc(2025, 1, 29, 10, 0)
            },
            new ApiModels.Lesson
            {
                StartTime = Instant.FromUtc(2025, 1, 29, 10, 30), 
                EndTime = Instant.FromUtc(2025, 1, 29, 11, 0)
            },
            new ApiModels.Lesson
            {
                StartTime = Instant.FromUtc(2025, 1, 29, 11, 10),
                EndTime = Instant.FromUtc(2025, 1, 29, 12, 00)
            }
        ]);
        
        var shutdownService = new ShutdownService(new Mock<ILogger<ShutdownService>>().Object, new FakeClock(initialInstant), apiManager.Object, new Mock<IPipeService>().Object);
        
        var duration = await shutdownService.GetDurationUntilBreakAsync();
        Assert.Equal(Duration.FromMinutes(90), duration);
    }
}