using System.Text;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure.Storage;

public class LocalBlobStorage : IBlobStorage
{
    private readonly string _root;

    public LocalBlobStorage(IConfiguration cfg)
    {
        _root = Path.Combine(AppContext.BaseDirectory, "local-blobs");
        Directory.CreateDirectory(_root);
    }

    public async Task<string> UploadAsync(string bucket, string objectName, Stream content, string contentType, CancellationToken ct = default)
    {
        var dir = Path.Combine(_root, bucket);
        Directory.CreateDirectory(dir);
        var fullPath = Path.Combine(dir, objectName.Replace("..", "").Replace("\\", "/").TrimStart('/'));
        var fullDir = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(fullDir);
        using (var fs = File.Create(fullPath))
        {
            await content.CopyToAsync(fs, ct);
        }
        return fullPath;
    }

    public Task<Stream> DownloadAsync(string bucket, string objectName, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, bucket, objectName.Replace("..", "").Replace("\\", "/").TrimStart('/'));
        Stream s = File.OpenRead(fullPath);
        return Task.FromResult(s);
    }

    public string GetPublicUrl(string bucket, string objectName)
    {
        return Path.Combine(_root, bucket, objectName.Replace("..", "").Replace("\\", "/").TrimStart('/'));
    }
}
