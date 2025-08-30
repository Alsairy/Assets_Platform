using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOcrJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ocr_jobs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    ProviderOpId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GcsInputUri = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    GcsOutputUri = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    LeaseOwner = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LeaseUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ocr_jobs", x => x.Id);
                    table.CheckConstraint("CK_ocr_jobs_Attempts_nonneg", "\"Attempts\" >= 0");
                    table.ForeignKey(
                        name: "FK_ocr_jobs_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_documents_OcrConfidence_0_1",
                table: "documents",
                sql: "\"OcrConfidence\" IS NULL OR (\"OcrConfidence\" >= 0 AND \"OcrConfidence\" <= 1)");

            migrationBuilder.CreateIndex(
                name: "IX_ocr_jobs_DocumentId_Status",
                table: "ocr_jobs",
                columns: new[] { "DocumentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ocr_jobs_ProviderOpId",
                table: "ocr_jobs",
                column: "ProviderOpId");

            migrationBuilder.CreateIndex(
                name: "IX_ocr_jobs_Status_LeaseUntil",
                table: "ocr_jobs",
                columns: new[] { "Status", "LeaseUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ocr_jobs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_documents_OcrConfidence_0_1",
                table: "documents");
        }
    }
}
