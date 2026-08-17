using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Node.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandWorklistItemFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "patient_birth_date",
                table: "worklist_items",
                type: "TEXT",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "patient_sex",
                table: "worklist_items",
                type: "TEXT",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "referring_physician_name",
                table: "worklist_items",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "requested_procedure_id",
                table: "worklist_items",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scheduled_performing_physician_name",
                table: "worklist_items",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scheduled_procedure_step_id",
                table: "worklist_items",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scheduled_station_ae_title",
                table: "worklist_items",
                type: "TEXT",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "study_instance_uid",
                table: "worklist_items",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_worklist_items_station_ae",
                table: "worklist_items",
                column: "scheduled_station_ae_title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_worklist_items_station_ae",
                table: "worklist_items");

            migrationBuilder.DropColumn(
                name: "patient_birth_date",
                table: "worklist_items");

            migrationBuilder.DropColumn(
                name: "patient_sex",
                table: "worklist_items");

            migrationBuilder.DropColumn(
                name: "referring_physician_name",
                table: "worklist_items");

            migrationBuilder.DropColumn(
                name: "requested_procedure_id",
                table: "worklist_items");

            migrationBuilder.DropColumn(
                name: "scheduled_performing_physician_name",
                table: "worklist_items");

            migrationBuilder.DropColumn(
                name: "scheduled_procedure_step_id",
                table: "worklist_items");

            migrationBuilder.DropColumn(
                name: "scheduled_station_ae_title",
                table: "worklist_items");

            migrationBuilder.DropColumn(
                name: "study_instance_uid",
                table: "worklist_items");
        }
    }
}
