using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

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
        _base = cfg["FLOWABLE__BASE_URL"] ?? "http://localhost:8080/flowable-rest";
        _http = new HttpClient();
        var u = cfg["FLOWABLE__USER"] ?? "flowable";
        var p = cfg["FLOWABLE__PASS"] ?? "flowable";
        var byteArray = System.Text.Encoding.ASCII.GetBytes($"{u}:{p}");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
    }
    public async Task<string> StartProcessAsync(string processKey, IDictionary<string, object> variables)
    {
        var url = $"{_base}/service/runtime/process-instances";
        var payload = new {
            processDefinitionKey = processKey,
            variables = variables.Select(kv => new { name = kv.Key, value = kv.Value })
        };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var res = await _http.PostAsync(url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        res.EnsureSuccessStatusCode();
        var txt = await res.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(txt);
        return doc.RootElement.GetProperty("id").GetString() ?? $"wf_{Guid.NewGuid():N}";
    }
}

public interface IOcrService
{
    Task<(bool success, string? text, Dictionary<string,string> extracted)> ProcessAsync(string filePath, string contentType);
}

public class GoogleVisionOcrService : IOcrService
{
    private readonly string? _apiKey;
    public GoogleVisionOcrService(IConfiguration cfg){ _apiKey = cfg["OCR__GOOGLE_API_KEY"]; }
    public async Task<(bool, string?, Dictionary<string,string>)> ProcessAsync(string filePath, string contentType)
    {
        if (string.IsNullOrEmpty(_apiKey)) return (false, null, new());
        // Minimal call (pseudo) — replace with official SDK when wiring
        var bytes = await File.ReadAllBytesAsync(filePath);
        var b64 = Convert.ToBase64String(bytes);
        var payload = new {
            requests = new [] {
                new { image = new { content = b64 }, features = new [] { new { type = "DOCUMENT_TEXT_DETECTION" } } }
            }
        };
        using var http = new HttpClient();
        var url = $"https://vision.googleapis.com/v1/images:annotate?key={_apiKey}";
        var resp = await http.PostAsync(url, new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));
        if (!resp.IsSuccessStatusCode) return (false, null, new());
        var txt = await resp.Content.ReadAsStringAsync();
        // Extract fullTextAnnotation.text
        var doc = System.Text.Json.JsonDocument.Parse(txt);
        var text = doc.RootElement.GetProperty("responses")[0].GetProperty("fullTextAnnotation").GetProperty("text").GetString();
        return (true, text, new()); // add regex extraction rules later
    }
}

public class AzureOcrService : IOcrService
{
    private readonly string? _endpoint;
    private readonly string? _key;
    public AzureOcrService(IConfiguration cfg)
    {
        _endpoint = cfg["OCR__AZURE_ENDPOINT"];
        _key = cfg["OCR__AZURE_KEY"];
    }
    public Task<(bool, string?, Dictionary<string,string>)> ProcessAsync(string filePath, string contentType)
    {
        return Task.FromResult<(bool, string?, Dictionary<string,string>)>((false, null, new())); // wire later with SDK
    }
}

public class FakeOcrService : IOcrService
{
    public Task<(bool success, string? text, Dictionary<string,string> extracted)> ProcessAsync(string filePath, string contentType)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        var result = new Dictionary<string,string>();
        var text = $"[FAKE OCR] {name}";
        if (name.Contains("DOC-"))
        {
            var idx = name.IndexOf("DOC-");
            var digits = new string(name.Substring(idx+4).TakeWhile(char.IsDigit).ToArray());
            if (!string.IsNullOrWhiteSpace(digits)) result["OwnershipDocumentNumber"] = $"DOC-{digits}";
        }
        return Task.FromResult((true, text, result));
    }
}
