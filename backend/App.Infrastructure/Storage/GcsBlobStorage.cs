using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure.Storage;

public class GcsBlobStorage : IBlobStorage
{
    private readonly StorageClient storage;
    private readonly string projectId;

    public GcsBlobStorage(StorageClient storage, IConfiguration cfg)
    {
        this.storage = storage;
        projectId = cfg["GoogleCloud:ProjectId"] ?? "";
    }

    public async Task<string> UploadAsync(string bucket, string objectName, Stream content, string contentType, CancellationToken ct = default)
    {
        var obj = await storage.UploadObjectAsync(bucket, objectName, contentType, content, cancellationToken: ct);
        return obj.MediaLink ?? $"gs://{bucket}/{objectName}";
    }

    public async Task<Stream> DownloadAsync(string bucket, string objectName, CancellationToken ct = default)
    {
        var ms = new MemoryStream();
        await storage.DownloadObjectAsync(bucket, objectName, ms, cancellationToken: ct);
        ms.Position = 0;
        return ms;
    }

    public string GetPublicUrl(string bucket, string objectName)
    {
        return $"https://storage.googleapis.com/{bucket}/{objectName}";
    }
}
