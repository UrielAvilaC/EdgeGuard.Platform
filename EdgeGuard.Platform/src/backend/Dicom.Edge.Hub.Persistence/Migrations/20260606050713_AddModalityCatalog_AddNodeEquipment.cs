using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModalityCatalog_AddNodeEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "modalities",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_supported = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modalities", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "node_equipment",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    node_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ae_title = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    station_ae_title = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    station_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    last_connection_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_equipment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "equipment_modalities",
                columns: table => new
                {
                    equipment_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    modality_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_modalities", x => new { x.equipment_id, x.modality_code });
                    table.ForeignKey(
                        name: "fk_equipment_modalities_modalities_modality_code",
                        column: x => x.modality_code,
                        principalTable: "modalities",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_equipment_modalities_node_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalTable: "node_equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_modalities_code",
                table: "equipment_modalities",
                column: "modality_code");

            migrationBuilder.CreateIndex(
                name: "ix_modalities_sort_order",
                table: "modalities",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "ix_node_equipment_node_ae",
                table: "node_equipment",
                columns: new[] { "node_id", "ae_title" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipment_modalities");

            migrationBuilder.DropTable(
                name: "modalities");

            migrationBuilder.DropTable(
                name: "node_equipment");
        }
    }
}
