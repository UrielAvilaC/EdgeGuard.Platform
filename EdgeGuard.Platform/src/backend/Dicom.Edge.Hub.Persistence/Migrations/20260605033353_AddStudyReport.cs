using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "report_content",
                table: "studies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "report_format",
                table: "studies",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "report_pdf_path",
                table: "studies",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "report_received_at",
                table: "studies",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "report_content",
                table: "studies");

            migrationBuilder.DropColumn(
                name: "report_format",
                table: "studies");

            migrationBuilder.DropColumn(
                name: "report_pdf_path",
                table: "studies");

            migrationBuilder.DropColumn(
                name: "report_received_at",
                table: "studies");
        }
    }
}
