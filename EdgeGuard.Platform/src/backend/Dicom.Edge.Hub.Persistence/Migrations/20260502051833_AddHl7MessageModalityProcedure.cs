using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHl7MessageModalityProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "modality",
                table: "hl7_messages",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "procedure_description",
                table: "hl7_messages",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "modality",
                table: "hl7_messages");

            migrationBuilder.DropColumn(
                name: "procedure_description",
                table: "hl7_messages");
        }
    }
}
