using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using NodaTime;
using WinService.Manager;
using WinService.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ClassInsights";
});

builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(10);
});

builder.Logging.AddEventLog(new EventLogSettings
{
    SourceName = "ClassInsights"
});

builder.Services.AddSingleton<IClock>(SystemClock.Instance);
builder.Services.AddSingleton<ApiManager>();
builder.Services.AddSingleton<PipeService>();

builder.Services.AddHostedService<PipeService>(provider => provider.GetRequiredService<PipeService>());
builder.Services.AddHostedService<HeartbeatService>();
builder.Services.AddHostedService<ShutdownService>();
builder.Services.AddHostedService<WebSocketService>();

var host = builder.Build();
host.Run();
