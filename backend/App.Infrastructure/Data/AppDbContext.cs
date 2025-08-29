using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AssetType> AssetTypes => Set<AssetType>();
    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetFieldValue> AssetFieldValues => Set<AssetFieldValue>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<FieldPermission> FieldPermissions => Set<FieldPermission>();
    public DbSet<OcrJob> OcrJobs => Set<OcrJob>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AssetType>().ToTable("asset_types");
        modelBuilder.Entity<FieldDefinition>().ToTable("field_definitions");
        modelBuilder.Entity<Asset>().ToTable("assets");
        modelBuilder.Entity<AssetFieldValue>().ToTable("asset_field_values");
        modelBuilder.Entity<Document>().ToTable("documents");
        modelBuilder.Entity<WorkflowInstance>().ToTable("workflow_instances");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<FieldPermission>().ToTable("field_permissions");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs");

        modelBuilder.Entity<FieldDefinition>()
            .HasOne(f => f.AssetType)
            .WithMany(t => t.Fields)
            .HasForeignKey(f => f.AssetTypeId);

        modelBuilder.Entity<AssetFieldValue>()
            .HasOne(v => v.Asset)
            .WithMany(a => a.FieldValues)
            .HasForeignKey(v => v.AssetId);

        modelBuilder.Entity<AssetFieldValue>()
            .HasOne(v => v.FieldDefinition)
            .WithMany()
            .HasForeignKey(v => v.FieldDefinitionId);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Asset)
            .WithMany(a => a.Documents)
            .HasForeignKey(d => d.AssetId);

        modelBuilder.Entity<Document>()
            .HasCheckConstraint("CK_documents_OcrConfidence_0_1",
                "\"OcrConfidence\" IS NULL OR (\"OcrConfidence\" >= 0 AND \"OcrConfidence\" <= 1)");

        modelBuilder.Entity<Document>()
            .HasIndex(d => d.AssetId);

        modelBuilder.Entity<Document>()
            .Property(d => d.OcrStatus)
            .HasConversion<string>();

        modelBuilder.Entity<OcrJob>().ToTable("ocr_jobs");
        modelBuilder.Entity<OcrJob>()
            .HasOne(j => j.Document)
            .WithMany()
            .HasForeignKey(j => j.DocumentId);
        modelBuilder.Entity<OcrJob>()
            .HasIndex(j => new { j.DocumentId, j.Status });
        modelBuilder.Entity<OcrJob>()
            .HasIndex(j => j.ProviderOpId);

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "Officer" },
            new Role { Id = 3, Name = "Reviewer" }
        );
    }
}
