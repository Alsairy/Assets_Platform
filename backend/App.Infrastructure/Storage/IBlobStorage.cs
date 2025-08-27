using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace App.Infrastructure.Storage;

public interface IBlobStorage
{
    Task<string> UploadAsync(string bucket, string objectName, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string bucket, string objectName, CancellationToken ct = default);
    string GetPublicUrl(string bucket, string objectName);
}
