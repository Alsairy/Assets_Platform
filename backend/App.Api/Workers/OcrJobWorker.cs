using App.Domain.Entities;
using App.Infrastructure.Data;
using App.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

public class OcrJobWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<OcrJobWorker> _logger;
    private readonly TimeSpan _poll = TimeSpan.FromSeconds(5);
    private readonly string _leaseOwner = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    public OcrJobWorker(IServiceProvider sp, ILogger<OcrJobWorker> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OcrJobWorker starting with lease owner {owner}", _leaseOwner);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var ocr = scope.ServiceProvider.GetRequiredService<IOcrService>();
                var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                var now = DateTime.UtcNow;
                var leaseFor = now.AddSeconds(30);

                var claimed = await db.OcrJobs
                    .Where(j => (j.Status == "Queued" || j.Status == "Processing") &&
                                (j.LeaseUntil == null || j.LeaseUntil < now))
                    .OrderBy(j => j.UpdatedAt)
                    .Take(1)
                    .ToListAsync(stoppingToken);

                if (claimed.Count == 0)
                {
                    await Task.Delay(_poll, stoppingToken);
                    continue;
                }

                var job = claimed[0];
                var updated = await db.OcrJobs
                    .Where(j => j.Id == job.Id &&
                                (j.LeaseUntil == null || j.LeaseUntil < now))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.LeaseOwner, _leaseOwner)
                        .SetProperty(p => p.LeaseUntil, leaseFor)
                        .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                        stoppingToken);

                if (updated == 0)
                {
                    continue;
                }

                job = await db.OcrJobs.Include(j => j.Document).FirstAsync(j => j.Id == job.Id, stoppingToken);

                if (job.Status == "Queued" && string.IsNullOrWhiteSpace(job.ProviderOpId))
                {
                    job.Attempts += 1;
                    var bucket = cfg["GCS:BucketName"] ?? "assets-dev";
                    var input = job.GcsInputUri ?? $"gs://{bucket}/{job.Document!.StoragePath}";
                    var (ok, providerId, err) = await ocr.StartPdfOcrAsync(input, stoppingToken);
                    if (!ok || string.IsNullOrWhiteSpace(providerId))
                    {
                        job.LastError = err ?? "failed to start";
                        job.Status = "Failed";
                    }
                    else
                    {
                        job.ProviderOpId = providerId;
                        job.Status = "Processing";
                        job.LastError = null;
                    }
                    job.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                    continue;
                }

                if (job.Status == "Processing" && !string.IsNullOrWhiteSpace(job.ProviderOpId))
                {
                    var (done, success, outputUri, meanConf, err) = await ocr.PollPdfOcrAsync(job.ProviderOpId!, stoppingToken);
                    if (!done)
                    {
                        continue;
                    }

                    var threshold = cfg.GetValue<double?>("OCR:ConfidenceThreshold") ?? 0.85;
                    var doc = await db.Documents.FirstAsync(d => d.Id == job.DocumentId, stoppingToken);

                    if (success)
                    {
                        job.Status = meanConf.HasValue && meanConf.Value < threshold ? "LowConfidence" : "Succeeded";
                        doc.OcrStatus = job.Status == "LowConfidence" ? OcrStatus.LowConfidence : OcrStatus.Succeeded;
                        doc.OcrConfidence = meanConf;
                        job.GcsOutputUri = outputUri;
                        job.LastError = null;
                    }
                    else
                    {
                        job.Status = "Failed";
                        doc.OcrStatus = OcrStatus.Failed;
                        job.LastError = err ?? "ocr failed";
                    }

                    job.LeaseOwner = null;
                    job.LeaseUntil = null;
                    job.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                    continue;
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker loop error");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
