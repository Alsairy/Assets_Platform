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

    private static string SafeJoin(string root, string relative)
    {
        var combined = Path.GetFullPath(Path.Combine(root, relative.Replace('\\','/').TrimStart('/')));
        var rootFull = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        if (!combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Invalid path.");
        return combined;
    }

    public async Task<string> UploadAsync(string bucket, string objectName, Stream content, string contentType, CancellationToken ct = default)
    {
        var dir = SafeJoin(_root, bucket);
        Directory.CreateDirectory(dir);
        var fullPath = SafeJoin(dir, objectName);
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
        var fullPath = SafeJoin(SafeJoin(_root, bucket), objectName);
        Stream s = File.OpenRead(fullPath);
        return Task.FromResult(s);
    }

    public string GetPublicUrl(string bucket, string objectName)
    {
        return SafeJoin(SafeJoin(_root, bucket), objectName);
    }
}
