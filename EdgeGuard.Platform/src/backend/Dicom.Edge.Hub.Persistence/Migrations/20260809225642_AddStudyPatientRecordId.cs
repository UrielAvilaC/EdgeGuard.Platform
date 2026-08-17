using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <summary>
    /// Adds the hard link from a study to its patient record. <c>studies.patient_id</c> keeps
    /// holding the MRN (provenance, and the key the HL7 merges use); the new FK is what the
    /// UI queries. Existing rows are matched by MRN — the previous migration guarantees the
    /// patient exists.
    /// </summary>
    /// <inheritdoc />
    public partial class AddStudyPatientRecordId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "patient_record_id",
                table: "studies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Backfill before the FK is validated. Only live patient records are linked;
            // studies with a placeholder or unknown MRN stay null, which is correct.
            migrationBuilder.Sql("""
                UPDATE studies s
                SET patient_record_id = p.id
                FROM patients p
                WHERE p.patient_dicom_id = s.patient_id
                  AND p.is_deleted = false
                  AND s.patient_record_id IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_studies_patient_record_id",
                table: "studies",
                column: "patient_record_id");

            migrationBuilder.AddForeignKey(
                name: "fk_studies_patients_patient_record_id",
                table: "studies",
                column: "patient_record_id",
                principalTable: "patients",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_studies_patients_patient_record_id",
                table: "studies");

            migrationBuilder.DropIndex(
                name: "ix_studies_patient_record_id",
                table: "studies");

            migrationBuilder.DropColumn(
                name: "patient_record_id",
                table: "studies");
        }
    }
}
