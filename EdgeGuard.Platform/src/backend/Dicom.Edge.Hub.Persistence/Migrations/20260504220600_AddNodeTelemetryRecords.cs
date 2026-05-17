using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeTelemetryRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "node_telemetry_records",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    node_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_associations = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    accepted_associations = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    rejected_associations = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    aborted_associations = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_images_received = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    completed_studies = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_bytes_received = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    average_reception_duration_ms = table.Column<double>(type: "double precision", nullable: true),
                    average_throughput_mbps = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_telemetry_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_node_telemetry_records_node_id",
                table: "node_telemetry_records",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_telemetry_records_reported_at",
                table: "node_telemetry_records",
                column: "reported_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "node_telemetry_records");
        }
    }
}
