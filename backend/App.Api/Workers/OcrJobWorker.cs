using App.Infrastructure.Data;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class OcrJobWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<OcrJobWorker> _log;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(2);
    private readonly string _workerId = Environment.MachineName + ":" + Guid.NewGuid().ToString("N");

    public OcrJobWorker(IServiceProvider sp, ILogger<OcrJobWorker> log) { _sp = sp; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.UtcNow;

                var candidateId = await db.OcrJobs
                    .Where(j => (j.Status == OcrStatus.Pending || j.Status == OcrStatus.Processing) &&
                                (j.LeaseUntil == null || j.LeaseUntil < now))
                    .OrderBy(j => j.Id)
                    .Select(j => j.Id)
                    .FirstOrDefaultAsync(stoppingToken);
                if (candidateId == 0)
                {
                    await Task.Delay(_interval, stoppingToken);
                    continue;
                }

                var leaseUntil = now.AddMinutes(2);
                var claimed = await db.OcrJobs
                    .Where(j => j.Id == candidateId && (j.LeaseUntil == null || j.LeaseUntil < now))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(j => j.LeaseOwner, _workerId)
                        .SetProperty(j => j.LeaseUntil, leaseUntil)
                        .SetProperty(j => j.UpdatedAt, now), stoppingToken);

                if (claimed == 0) continue;

                var job = await db.OcrJobs.FirstAsync(j => j.Id == candidateId, stoppingToken);

                if (job.Status == OcrStatus.Pending)
                {
                    job.Status = OcrStatus.Processing;
                    job.Attempts += 1;
                    job.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);

                    var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == job.DocumentId, stoppingToken);
                    if (doc != null && doc.OcrStatus != OcrStatus.Processing)
                    {
                        doc.OcrStatus = OcrStatus.Processing;
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _log.LogError(ex, "OcrJobWorker loop error");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
