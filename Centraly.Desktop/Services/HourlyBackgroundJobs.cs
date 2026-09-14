using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Centraly.Desktop.Services;

// Desktop's replacement for the two Hangfire recurring jobs (Program.cs in Api): no
// separate job server, just a timer inside the same process. Same underlying methods,
// same cadence - only the scheduler changed.
public class HourlyBackgroundJobs(IServiceRunner runner, ILogger<HourlyBackgroundJobs> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        do
        {
            try
            {
                await runner.RunAsync<INotificationBackgroundJobs>(j => j.CheckMaintenanceTicketsAsync(stoppingToken));
                await runner.RunAsync<INotificationBackgroundJobs>(j => j.CheckDrawerSessionsLeftOpenAsync(stoppingToken));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Hourly background job run failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
