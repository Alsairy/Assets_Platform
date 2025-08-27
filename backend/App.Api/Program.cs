using App.Domain.Entities;
using App.Infrastructure.Data;
using App.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

// Db
var conn = builder.Configuration.GetConnectionString("Default") ?? builder.Configuration["ConnectionStrings:Default"];
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(conn));

// Services (choose OCR provider by env)
var ocrProvider = builder.Configuration["OCR__PROVIDER"] ?? "Fake";
if (ocrProvider.Equals("Google", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IOcrService, GoogleVisionOcrService>();
else if (ocrProvider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IOcrService, AzureOcrService>();
else
    builder.Services.AddSingleton<IOcrService, FakeOcrService>();

builder.Services.AddSingleton<IWorkflowEngine, FlowableWorkflowEngineAdapter>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "DAMP API", Version = "v1" }));

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

// fake auth: read role + region from headers; replace with JwtBearer later
(string role, string region, string city) GetContext(HttpRequest req) =>
    (req.Headers.TryGetValue("X-User-Role", out var r) ? r.ToString() : "Admin",
     req.Headers.TryGetValue("X-Region", out var rg) ? rg.ToString() : "",
     req.Headers.TryGetValue("X-City", out var cg) ? cg.ToString() : "");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsDevelopment())
    {
        db.Database.Migrate();
        await AppDbSeeder.SeedAsync(db);
    }
}

// Health
app.MapGet("/health", () => Results.Ok(new { status = "ok", ts = DateTimeOffset.UtcNow }));

// ---- Admin: Asset Types & Fields ----
app.MapPost("/api/asset-types", async (AppDbContext db, AssetType input) =>
{
    input.Id = 0;
    db.AssetTypes.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/api/asset-types/{input.Id}", input);
});

app.MapGet("/api/asset-types", async (AppDbContext db) =>
    Results.Ok(await db.AssetTypes.Include(t => t.Fields).OrderByDescending(x => x.Id).ToListAsync()));

app.MapPost("/api/fields", async (AppDbContext db, FieldDefinition input) =>
{
    input.Id = 0;
    db.FieldDefinitions.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/api/fields/{input.Id}", input);
});

app.MapGet("/api/fields/by-type/{assetTypeId:long}", async (AppDbContext db, long assetTypeId, HttpRequest req) =>
{
    var (role, _, _) = GetContext(req);
    var fields = await db.FieldDefinitions.Where(f => f.AssetTypeId == assetTypeId).ToListAsync();
    if (role != "Admin") fields = fields.Where(f => f.PrivacyLevel != PrivacyLevel.Restricted).ToList();
    return Results.Ok(fields);
});

// ---- Field Permissions ----
app.MapPost("/api/field-permissions", async (AppDbContext db, FieldPermission p) =>
{
    p.Id = 0;
    db.FieldPermissions.Add(p);
    await db.SaveChangesAsync();
    return Results.Ok(p);
});

// ---- Assets CRUD ----

app.MapPost("/api/assets", async (AppDbContext db, IWorkflowEngine wf, CreateAssetDto dto) =>
{
    var t = await db.AssetTypes.Include(t => t.Fields).FirstOrDefaultAsync(t => t.Id == dto.AssetTypeId);
    if (t == null) return Results.BadRequest("AssetType not found");

    var a = new Asset {
        AssetTypeId = t.Id,
        Name = dto.Name,
        Region = dto.Region,
        City = dto.City,
        Latitude = dto.Latitude,
        Longitude = dto.Longitude,
        Status = "Draft"
    };
    db.Assets.Add(a);
    await db.SaveChangesAsync();

    if (dto.FieldValues != null)
    {
        foreach (var kv in dto.FieldValues)
        {
            var f = t.Fields.FirstOrDefault(x => x.Name == kv.Key);
            if (f != null)
                db.AssetFieldValues.Add(new AssetFieldValue { AssetId = a.Id, FieldDefinitionId = f.Id, Value = kv.Value });
        }
        await db.SaveChangesAsync();
    }

    // Start registration workflow
    var wfId = await wf.StartProcessAsync("asset_registration", new Dictionary<string,object> {
        ["assetId"] = a.Id, ["assetType"] = t.Name, ["region"] = a.Region, ["city"] = a.City
    });
    a.WorkflowInstanceId = wfId;
    a.Status = "InWorkflow";
    await db.SaveChangesAsync();

    return Results.Created($"/api/assets/{a.Id}", a);
});

// list with filters + pagination
app.MapGet("/api/assets", async (AppDbContext db, HttpRequest req, int page = 1, int pageSize = 20, long? assetTypeId = null, string? q = null, string? region = null, string? city = null, string? status = null) =>
{
    var (role, rscope, cscope) = GetContext(req);
    var query = db.Assets.AsQueryable();
    if (assetTypeId.HasValue) query = query.Where(a => a.AssetTypeId == assetTypeId);
    if (!string.IsNullOrWhiteSpace(q)) query = query.Where(a => EF.Functions.ILike(a.Name, $"%{q}%"));
    if (!string.IsNullOrWhiteSpace(region)) query = query.Where(a => a.Region == region);
    if (!string.IsNullOrWhiteSpace(city)) query = query.Where(a => a.City == city);
    if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
    // scope enforcement
    if (!string.IsNullOrEmpty(rscope)) query = query.Where(a => a.Region == rscope);
    if (!string.IsNullOrEmpty(cscope)) query = query.Where(a => a.City == cscope);

    var total = await query.CountAsync();
    var items = await query.OrderByDescending(a => a.Id).Skip((page-1)*pageSize).Take(pageSize).Select(a => new {
        a.Id, a.Name, a.Region, a.City, a.Status, a.AssetTypeId
    }).ToListAsync();

    return Results.Ok(new { total, items });
});

app.MapGet("/api/assets/{id:long}", async (AppDbContext db, long id, HttpRequest req) =>
{
    var (role, rscope, cscope) = GetContext(req);
    var a = await db.Assets.Include(a => a.AssetType)!.ThenInclude(t => t!.Fields)
        .Include(a => a.FieldValues)
        .Include(a => a.Documents)
        .FirstOrDefaultAsync(x => x.Id == id);
    if (a == null) return Results.NotFound();
    if (!string.IsNullOrEmpty(rscope) && a.Region != rscope) return Results.Forbid();
    if (!string.IsNullOrEmpty(cscope) && a.City != cscope) return Results.Forbid();

    var fields = a.FieldValues.Select(v => new {
        Field = a.AssetType!.Fields.First(f => f.Id == v.FieldDefinitionId),
        v.Value
    })
    .Where(x => role == "Admin" || x.Field.PrivacyLevel != PrivacyLevel.Restricted)
    .Select(x => new { x.Field.Name, x.Field.DataType, x.Field.PrivacyLevel, x.Value })
    .ToList();

    return Results.Ok(new {
        a.Id, a.Name, a.Region, a.City, a.Latitude, a.Longitude, a.Status, a.WorkflowInstanceId,
        Fields = fields,
        Docs = a.Documents.Select(d => new { d.Id, d.FileName, d.ContentType, d.Version, d.UploadedUtc })
    });
});

app.MapPut("/api/assets/{id:long}", async (AppDbContext db, long id, UpdateAssetDto dto) =>
{
    var a = await db.Assets.Include(x=>x.AssetType)!.ThenInclude(t=>t!.Fields).FirstOrDefaultAsync(x => x.Id == id);
    if (a == null) return Results.NotFound();
    if (!string.IsNullOrWhiteSpace(dto.Name)) a.Name = dto.Name!;
    if (!string.IsNullOrWhiteSpace(dto.Region)) a.Region = dto.Region!;
    if (!string.IsNullOrWhiteSpace(dto.City)) a.City = dto.City!;
    if (dto.Latitude.HasValue) a.Latitude = dto.Latitude;
    if (dto.Longitude.HasValue) a.Longitude = dto.Longitude;
    a.UpdatedUtc = DateTime.UtcNow;
    await db.SaveChangesAsync();

    if (dto.FieldValues != null)
    {
        foreach (var kv in dto.FieldValues)
        {
            var fd = a.AssetType!.Fields.FirstOrDefault(f => f.Name == kv.Key);
            if (fd == null) continue;
            var existing = await db.AssetFieldValues.FirstOrDefaultAsync(v => v.AssetId == a.Id && v.FieldDefinitionId == fd.Id);
            if (existing != null) existing.Value = kv.Value;
            else db.AssetFieldValues.Add(new AssetFieldValue { AssetId = a.Id, FieldDefinitionId = fd.Id, Value = kv.Value });
        }
        await db.SaveChangesAsync();
    }
    return Results.Ok(a);
});

// ---- Document upload with versioning + OCR ----
app.MapPost("/api/assets/{id:long}/documents", async (AppDbContext db, IOcrService ocr, long id, HttpRequest req) =>
{
    var a = await db.Assets.Include(x=>x.AssetType)!.ThenInclude(t=>t!.Fields).FirstOrDefaultAsync(x => x.Id == id);
    if (a == null) return Results.NotFound();
    if (!req.Form.Files.Any()) return Results.BadRequest("No file");

    var f = req.Form.Files[0];
    var dir = Path.Combine(AppContext.BaseDirectory, "uploads", id.ToString());
    Directory.CreateDirectory(dir);
    var existingVersions = await db.Documents.Where(d => d.AssetId == id && d.FileName == f.FileName).CountAsync();
    var version = existingVersions + 1;
    var path = Path.Combine(dir, $"{version}_{f.FileName}");
    using (var fs = File.Create(path)) { await f.CopyToAsync(fs); }

    var doc = new Document { AssetId = id, FileName = f.FileName, ContentType = f.ContentType, StoragePath = path, Version = version };
    db.Documents.Add(doc);
    await db.SaveChangesAsync();

    var (ok, text, extracted) = await ocr.ProcessAsync(path, f.ContentType);
    if (ok)
    {
        doc.OcrText = text;
        foreach (var kv in extracted)
        {
            var fd = a.AssetType!.Fields.FirstOrDefault(ff => ff.Name == kv.Key);
            if (fd != null)
            {
                var existing = await db.AssetFieldValues.FirstOrDefaultAsync(v => v.AssetId == a.Id && v.FieldDefinitionId == fd.Id);
                if (existing != null) existing.Value = kv.Value;
                else db.AssetFieldValues.Add(new AssetFieldValue { AssetId = a.Id, FieldDefinitionId = fd.Id, Value = kv.Value });
            }
        }
        await db.SaveChangesAsync();
    }
    return Results.Created($"/api/assets/{id}/documents/{doc.Id}", new { doc.Id, doc.FileName, doc.Version, doc.UploadedUtc });
})
.Accepts<IFormFile>("multipart/form-data");

// ---- Reports ----
app.MapGet("/api/reports/portfolio", async (AppDbContext db) =>
{
    var total = await db.Assets.CountAsync();
    var byRegion = await db.Assets.GroupBy(a => a.Region).Select(g => new { region = g.Key, count = g.Count() }).ToListAsync();
    var byType = await db.Assets.GroupBy(a => a.AssetTypeId).Select(g => new { assetTypeId = g.Key, count = g.Count() }).ToListAsync();
    return Results.Ok(new { total, byRegion, byType });
});

app.Run();
public record CreateAssetDto(long AssetTypeId, string Name, string Region, string City, double? Latitude, double? Longitude, Dictionary<string,string>? FieldValues);
public record UpdateAssetDto(string? Name, string? Region, string? City, double? Latitude, double? Longitude, Dictionary<string,string>? FieldValues);
