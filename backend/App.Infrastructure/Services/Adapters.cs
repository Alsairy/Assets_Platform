using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Polly;
using Google.Cloud.Vision.V1;

namespace App.Infrastructure.Services;

public interface IWorkflowEngine
{
    Task<string> StartProcessAsync(string processKey, IDictionary<string, object> variables);
}

public class FlowableWorkflowEngineAdapter : IWorkflowEngine
{
    private readonly HttpClient _http;
    private readonly string _base;
    public FlowableWorkflowEngineAdapter(IConfiguration cfg)
    {
        _base = cfg["Flowable:BaseUrl"] ?? "http://localhost:8080/flowable-rest";
        _http = new HttpClient { BaseAddress = new Uri(_base), Timeout = TimeSpan.FromSeconds(15) };
        var u = cfg["Flowable:Username"] ?? "flowable";
        var p = cfg["Flowable:Password"] ?? "flowable";
        var byteArray = System.Text.Encoding.ASCII.GetBytes($"{u}:{p}");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
    }
    public async Task<string> StartProcessAsync(string processKey, IDictionary<string, object> variables)
    {
        var url = "/service/runtime/process-instances";
        var businessKey = variables.TryGetValue("assetId", out var idObj) ? idObj?.ToString() : Guid.NewGuid().ToString("N");
        var payload = new {
            processDefinitionKey = processKey,
            businessKey,
            variables = variables.Select(kv => new { name = kv.Key, value = kv.Value })
        };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);

        var rnd = new Random();
        var retry = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult(r => (int)r.StatusCode == 429 || (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(Math.Pow(2, i)) + TimeSpan.FromMilliseconds(rnd.Next(0, 250)));

        var res = await retry.ExecuteAsync(() => _http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json")));
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            throw new Exception($"Flowable error {res.StatusCode}: {body}");
        }

        var txt = await res.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(txt);
        return doc.RootElement.GetProperty("id").GetString() ?? $"wf_{Guid.NewGuid():N}";
    }
}

public interface IOcrService
{
    Task<(bool success, string? text, Dictionary<string,string> extracted, double? confidence)> ProcessAsync(string bucket, string objectName, string contentType, CancellationToken ct = default);
}

public class GoogleVisionOcrService : IOcrService
{
    private readonly ImageAnnotatorClient _vision;
    private readonly IConfiguration _cfg;
    private readonly double _threshold;
    private readonly string[] _hints;

    public GoogleVisionOcrService(ImageAnnotatorClient vision, IConfiguration cfg)
    {
        _vision = vision; _cfg = cfg;
        _threshold = _cfg.GetValue<double?>("OCR:ConfidenceThreshold") ?? 0.8;
        _hints = _cfg.GetSection("OCR:LanguageHints").Get<string[]>() ?? new[] { "ar", "en" };
    }

    public async Task<(bool success, string? text, Dictionary<string,string> extracted, double? confidence)> ProcessAsync(
        string bucket, string objectName, string contentType, CancellationToken ct = default)
    {
        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return (false, null, new(), null);

        var img = Image.FromUri($"gs://{bucket}/{objectName}");
        var req = new AnnotateImageRequest {
            Image = img,
            Features = { new Feature { Type = Feature.Types.Type.DocumentTextDetection } },
            ImageContext = new ImageContext { LanguageHints = { _hints } }
        };

        var resp = await _vision.AnnotateAsync(req, ct);
        if (resp?.Error is not null && resp.Error.Code != 0)
            return (false, null, new(), null);

        var anno = resp.FullTextAnnotation;
        if (anno is null || string.IsNullOrWhiteSpace(anno.Text))
            return (false, null, new(), null);

        double? conf = null;
        if (anno.Pages != null && anno.Pages.Count > 0)
        {
            var list = anno.Pages.SelectMany(p => p.Blocks).Select(b => (double?)b.Confidence).Where(x => x.HasValue).ToList();
            conf = list.Count == 0 ? null : list.Average();
        }
        var extracted = Extract(anno.Text);
        var ok = conf == null || conf >= _threshold;
        return (ok, anno.Text, extracted, conf);
    }

    private static readonly Regex DocRegex = new(@"\bDOC[-\s]*([0-9]{3,})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex DateRegex = new(@"\b(20\d{2})[-/](\d{1,2})[-/](\d{1,2})\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static string NormalizeDigits(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            sb.Append(ch switch
            {
                '\u0660' => '0', '\u0661' => '1', '\u0662' => '2', '\u0663' => '3', '\u0664' => '4',
                '\u0665' => '5', '\u0666' => '6', '\u0667' => '7', '\u0668' => '8', '\u0669' => '9',
                _ => ch
            });
        }
        return sb.ToString();
    }

    private static Dictionary<string,string> Extract(string text)
    {
        var map = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        var norm = NormalizeDigits(text);

        var doc = DocRegex.Match(norm);
        if (doc.Success) map["OwnershipDocumentNumber"] = $"DOC-{doc.Groups[1].Value}";

        var date = DateRegex.Match(norm);
        if (date.Success)
        {
            var y = date.Groups[1].Value;
            var m = date.Groups[2].Value.PadLeft(2, '0');
            var d = date.Groups[3].Value.PadLeft(2, '0');
            map["DocumentDate"] = $"{y}-{m}-{d}";
        }
        return map;
    }
}

public class AzureOcrService : IOcrService
{
    public Task<(bool success, string? text, Dictionary<string,string> extracted, double? confidence)> ProcessAsync(
        string bucket, string objectName, string contentType, CancellationToken ct = default)
    {
        return Task.FromResult<(bool success, string? text, Dictionary<string,string> extracted, double? confidence)>(
            (false, (string?)null, new Dictionary<string,string>(), (double?)null));
    }
}

public class FakeOcrService : IOcrService
{
    public Task<(bool success, string? text, Dictionary<string,string> extracted, double? confidence)> ProcessAsync(
        string bucket, string objectName, string contentType, CancellationToken ct = default)
    {
        var name = System.IO.Path.GetFileNameWithoutExtension(objectName);
        var result = new Dictionary<string,string>();
        var text = $"[FAKE OCR] {name}";
        if (name.Contains("DOC-"))
        {
            var idx = name.IndexOf("DOC-");
            var digits = new string(name.Substring(idx+4).TakeWhile(char.IsDigit).ToArray());
            if (!string.IsNullOrWhiteSpace(digits)) result["OwnershipDocumentNumber"] = $"DOC-{digits}";
        }
        return Task.FromResult<(bool success, string? text, Dictionary<string,string> extracted, double? confidence)>((true, text, result, (double?)null));
    }
}
