using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeDicomRoutingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "node_dicom_routing_rules",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    node_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    match_modality = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    match_source_ae_title = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    match_institution = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    match_study_desc = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    min_instance_count = table.Column<int>(type: "integer", nullable: true),
                    max_instance_count = table.Column<int>(type: "integer", nullable: true),
                    destination_ae_title = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    send_to_pacs = table.Column<bool>(type: "boolean", nullable: false),
                    send_to_hub = table.Column<bool>(type: "boolean", nullable: false),
                    anonymize_before_send = table.Column<bool>(type: "boolean", nullable: false),
                    match_count = table.Column<int>(type: "integer", nullable: false),
                    last_matched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_dicom_routing_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_node_dicom_routing_rules_node_active",
                table: "node_dicom_routing_rules",
                columns: new[] { "node_id", "is_enabled", "priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "node_dicom_routing_rules");
        }
    }
}
