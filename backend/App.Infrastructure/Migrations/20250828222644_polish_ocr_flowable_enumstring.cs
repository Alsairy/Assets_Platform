using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class polish_ocr_flowable_enumstring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_documents_OcrConfidence_0_1",
                table: "documents",
                sql: "\"OcrConfidence\" IS NULL OR (\"OcrConfidence\" >= 0 AND \"OcrConfidence\" <= 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_documents_OcrConfidence_0_1",
                table: "documents");
        }
    }
}
