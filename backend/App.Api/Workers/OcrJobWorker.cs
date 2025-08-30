using System.Net;
using App.Infrastructure.Data;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class OcrJobWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<OcrJobWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
    private readonly string _workerId = Environment.MachineName;

    public OcrJobWorker(IServiceProvider sp, ILogger<OcrJobWorker> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.UtcNow;

                var job = await db.OcrJobs
                    .Where(j => (j.Status == "Queued" || j.Status == "Processing") &&
                                (j.LeaseUntil == null || j.LeaseUntil < now))
                    .OrderBy(j => j.Id)
                    .FirstOrDefaultAsync(stoppingToken);

                if (job == null)
                {
                    await Task.Delay(_interval, stoppingToken);
                    continue;
                }

                job.LeaseOwner = _workerId;
                job.LeaseUntil = now.AddMinutes(2);
                job.UpdatedAt = now;
                await db.SaveChangesAsync(stoppingToken);

                if (job.Status == "Queued")
                {
                    job.Status = "Processing";
                    job.Attempts += 1;
                    job.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Worker loop error");
                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
