using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StartService;

// running as service
if (!Environment.UserInteractive && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((_, services) => { services.AddHostedService<Service>(); })
        .UseWindowsService(options => { options.ServiceName = "ClassInsights"; }).Build().Run();
else StartService.StartService.RunAsync(new CancellationToken()).GetAwaiter().GetResult();
