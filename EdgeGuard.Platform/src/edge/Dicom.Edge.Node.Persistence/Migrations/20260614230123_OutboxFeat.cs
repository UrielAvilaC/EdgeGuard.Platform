using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Node.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OutboxFeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "equipment",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ae_title = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    display_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    station_ae_title = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    station_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ip_address = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "modalities",
                columns: table => new
                {
                    code = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    display_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    is_supported = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modalities", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "equipment_modalities",
                columns: table => new
                {
                    equipment_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    modality_code = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_modalities", x => new { x.equipment_id, x.modality_code });
                    table.ForeignKey(
                        name: "FK_equipment_modalities_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_equipment_ae_title",
                table: "equipment",
                column: "ae_title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_equipment_modalities_code",
                table: "equipment_modalities",
                column: "modality_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipment_modalities");

            migrationBuilder.DropTable(
                name: "modalities");

            migrationBuilder.DropTable(
                name: "equipment");
        }
    }
}
