using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHl7MrgObxSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_image_links",
                table: "studies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "merged_into_patient_id",
                table: "patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_links_json",
                table: "hl7_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mrg_prior_accession_number",
                table: "hl7_messages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mrg_prior_patient_id",
                table: "hl7_messages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mrg_prior_patient_name",
                table: "hl7_messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_patients_merged_into",
                table: "patients",
                column: "merged_into_patient_id",
                filter: "merged_into_patient_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_patients_merged_into",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "external_image_links",
                table: "studies");

            migrationBuilder.DropColumn(
                name: "merged_into_patient_id",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "image_links_json",
                table: "hl7_messages");

            migrationBuilder.DropColumn(
                name: "mrg_prior_accession_number",
                table: "hl7_messages");

            migrationBuilder.DropColumn(
                name: "mrg_prior_patient_id",
                table: "hl7_messages");

            migrationBuilder.DropColumn(
                name: "mrg_prior_patient_name",
                table: "hl7_messages");
        }
    }
}
