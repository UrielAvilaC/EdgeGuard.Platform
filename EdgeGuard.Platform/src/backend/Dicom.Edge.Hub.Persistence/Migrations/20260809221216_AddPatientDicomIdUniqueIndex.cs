using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <summary>
    /// Makes <c>patient_dicom_id</c> unique among live records and repairs the catalogue:
    /// duplicates are consolidated and every study whose patient was never registered
    /// (walk-in studies that arrived without an HL7 order) gets its patient created.
    /// </summary>
    /// <inheritdoc />
    public partial class AddPatientDicomIdUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Consolidate duplicates before the unique index can be created ──
            // Carry any demographics/contact data the duplicates hold onto the survivor
            // (the earliest record), so consolidation never loses a phone or an email.
            migrationBuilder.Sql("""
                UPDATE patients s
                SET phone_number = COALESCE(s.phone_number, d.phone_number),
                    email        = COALESCE(s.email,        d.email),
                    birth_date   = COALESCE(s.birth_date,   d.birth_date),
                    sex          = COALESCE(s.sex,          d.sex)
                FROM (
                    SELECT patient_dicom_id,
                           (array_remove(array_agg(phone_number ORDER BY created_at), NULL))[1] AS phone_number,
                           (array_remove(array_agg(email        ORDER BY created_at), NULL))[1] AS email,
                           (array_remove(array_agg(birth_date   ORDER BY created_at), NULL))[1] AS birth_date,
                           (array_remove(array_agg(sex          ORDER BY created_at), NULL))[1] AS sex
                    FROM patients
                    WHERE is_deleted = false
                    GROUP BY patient_dicom_id
                    HAVING COUNT(*) > 1
                ) d
                WHERE s.patient_dicom_id = d.patient_dicom_id
                  AND s.is_deleted = false;
                """);

            // Soft-delete every duplicate but the earliest one. Studies reference the
            // patient by MRN (studies.patient_id), never by patients.id, so nothing breaks.
            migrationBuilder.Sql("""
                UPDATE patients p
                SET is_deleted = true,
                    deleted_at = NOW(),
                    is_active  = false
                FROM (
                    SELECT id
                    FROM (
                        SELECT id,
                               ROW_NUMBER() OVER (
                                   PARTITION BY patient_dicom_id
                                   ORDER BY created_at, id
                               ) AS rn
                        FROM patients
                        WHERE is_deleted = false
                    ) ranked
                    WHERE ranked.rn > 1
                ) dup
                WHERE p.id = dup.id;
                """);

            // ── 2. Backfill patients referenced by studies but absent from the catalogue ──
            // Mirrors IdGenerator.NewId(): "yyyyMMddHHmmssfff-<12 hex chars>".
            migrationBuilder.Sql("""
                INSERT INTO patients (
                    id, patient_dicom_id, patient_name, created_by_node_id,
                    is_active, is_deleted, last_updated_at, created_at, updated_at)
                SELECT to_char(NOW() AT TIME ZONE 'utc', 'YYYYMMDDHH24MISSMS')
                           || '-'
                           || substr(md5(random()::text || clock_timestamp()::text), 1, 12),
                       d.patient_id,
                       d.patient_name,
                       d.source_node_id,
                       true, false, NOW(), NOW(), NOW()
                FROM (
                    SELECT DISTINCT ON (s.patient_id)
                           s.patient_id,
                           COALESCE(NULLIF(btrim(s.patient_name), ''), s.patient_id) AS patient_name,
                           s.source_node_id
                    FROM studies s
                    WHERE s.is_deleted = false
                      AND s.patient_id IS NOT NULL
                      AND btrim(s.patient_id) <> ''
                      AND upper(btrim(s.patient_id)) NOT IN ('UNKNOWN', 'ANONYMOUS', 'NA', 'N/A')
                      AND NOT EXISTS (
                          SELECT 1 FROM patients p
                          WHERE p.patient_dicom_id = s.patient_id
                            AND p.is_deleted = false
                      )
                    ORDER BY s.patient_id, s.created_at DESC
                ) d;
                """);

            // ── 3. Unique index ───────────────────────────────────────────────────
            migrationBuilder.DropIndex(
                name: "ix_patients_patient_dicom_id",
                table: "patients");

            migrationBuilder.CreateIndex(
                name: "ux_patients_patient_dicom_id",
                table: "patients",
                column: "patient_dicom_id",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only the index is reverted: the consolidation and the backfill are data
            // repairs and are deliberately not undone.
            migrationBuilder.DropIndex(
                name: "ux_patients_patient_dicom_id",
                table: "patients");

            migrationBuilder.CreateIndex(
                name: "ix_patients_patient_dicom_id",
                table: "patients",
                column: "patient_dicom_id");
        }
    }
}
