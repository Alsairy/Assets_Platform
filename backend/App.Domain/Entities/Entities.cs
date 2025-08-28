namespace App.Domain.Entities;

public enum DataType { Text, Number, Date, Dropdown, Attachment, Location }
public enum PrivacyLevel { Public, Confidential, Restricted }
public enum OcrStatus { Pending, Succeeded, Failed, LowConfidence }


public class AssetType
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<FieldDefinition> Fields { get; set; } = new();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class FieldDefinition
{
    public long Id { get; set; }
    public long AssetTypeId { get; set; }
    public AssetType? AssetType { get; set; }
    public string Name { get; set; } = string.Empty;
    public DataType DataType { get; set; }
    public bool Required { get; set; } = false;
    public string? OwnerDepartment { get; set; }
    public PrivacyLevel PrivacyLevel { get; set; } = PrivacyLevel.Public;
    public string? OptionsCsv { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class Asset
{
    public long Id { get; set; }
    public long AssetTypeId { get; set; }
    public AssetType? AssetType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Status { get; set; } = "Draft";
    public string? WorkflowInstanceId { get; set; }
    public List<AssetFieldValue> FieldValues { get; set; } = new();
    public List<Document> Documents { get; set; } = new();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
}

public class AssetFieldValue
{
    public long Id { get; set; }
    public long AssetId { get; set; }
    public Asset? Asset { get; set; }
    public long FieldDefinitionId { get; set; }
    public FieldDefinition? FieldDefinition { get; set; }
    public string? Value { get; set; } // store as string; interpret by DataType
}

public class Document
{
    public long Id { get; set; }
    public long AssetId { get; set; }
    public Asset? Asset { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string StoragePath { get; set; } = string.Empty;
    public string? OcrText { get; set; }
    public OcrStatus? OcrStatus { get; set; }
    public double? OcrConfidence { get; set; }
    public int Version { get; set; } = 1;
    public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;
}

public class WorkflowInstance
{
    public long Id { get; set; }
    public long AssetId { get; set; }
    public string ProcessDefinitionKey { get; set; } = "";
    public string ProcessInstanceId { get; set; } = "";
    public string Status { get; set; } = "STARTED";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public class Role
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty; // Admin, Officer, Reviewer
    // Scoping
    public string? RegionScope { get; set; } // e.g., "Riyadh"; null = global
    public string? CityScope { get; set; }
}

public class FieldPermission
{
    public long Id { get; set; }
    public long RoleId { get; set; }
    public Role? Role { get; set; }
    public long FieldDefinitionId { get; set; }
    public FieldDefinition? FieldDefinition { get; set; }
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }
}

public class AuditLog
{
    public long Id { get; set; }
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public long? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime TsUtc { get; set; } = DateTime.UtcNow;
}
