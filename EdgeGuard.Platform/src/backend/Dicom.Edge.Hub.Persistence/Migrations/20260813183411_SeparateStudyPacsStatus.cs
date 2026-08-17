using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeparateStudyPacsStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pacs_status",
                table: "studies",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "NotQueued");

            migrationBuilder.CreateIndex(
                name: "ix_studies_pacs_status",
                table: "studies",
                column: "pacs_status");

            // ── Split the single status column into its two axes ──────────────────
            // 1. Move the PACS-send value of every study that carried one.
            migrationBuilder.Sql("""
                UPDATE studies SET pacs_status = CASE status
                    WHEN 'QueuedForSend' THEN 'Queued'
                    WHEN 'Sending'       THEN 'Sending'
                    WHEN 'SentToPacs'    THEN 'Sent'
                    WHEN 'Failed'        THEN 'Failed'
                    ELSE 'NotQueued'
                END;
                """);

            // 2. Give those same studies a clinical status, derived from the artifacts they
            //    actually hold — the same rule Study.RecomputeCompletion applies. Without this
            //    they would keep a PACS value in a column that now only accepts clinical ones.
            migrationBuilder.Sql("""
                UPDATE studies SET status = CASE
                    WHEN external_image_links IS NOT NULL AND external_image_links <> ''
                         AND (report_format <> 'None' OR report_pdf_path IS NOT NULL)
                         THEN 'Finalized'
                    WHEN report_format <> 'None' OR report_pdf_path IS NOT NULL
                         THEN 'WaitingForImageLinks'
                    WHEN external_image_links IS NOT NULL AND external_image_links <> ''
                         THEN 'WaitingForReport'
                    ELSE 'Completed'
                END
                WHERE status IN ('QueuedForSend', 'Sending', 'SentToPacs', 'Failed');
                """);

            // 3. Auto-send rules bound to a PACS status can no longer match anything, because
            //    those names never appear on the clinical axis again. Disabling beats leaving
            //    them enabled and silently inert — the operator sees them off and re-binds them.
            migrationBuilder.Sql("""
                UPDATE notification_auto_send_rules
                SET is_enabled = false
                WHERE study_status IN ('QueuedForSend', 'Sending', 'SentToPacs', 'Failed');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_studies_pacs_status",
                table: "studies");

            migrationBuilder.DropColumn(
                name: "pacs_status",
                table: "studies");
        }
    }
}
