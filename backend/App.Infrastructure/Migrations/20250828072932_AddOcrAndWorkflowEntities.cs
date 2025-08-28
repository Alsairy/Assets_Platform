using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOcrAndWorkflowEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "OcrConfidence",
                table: "documents",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrStatus",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "workflow_instances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssetId = table.Column<long>(type: "bigint", nullable: false),
                    ProcessDefinitionKey = table.Column<string>(type: "text", nullable: false),
                    ProcessInstanceId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_instances", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_instances");

            migrationBuilder.DropColumn(
                name: "OcrConfidence",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "OcrStatus",
                table: "documents");
        }
    }
}
