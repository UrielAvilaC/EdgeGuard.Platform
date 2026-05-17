using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Node.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNodePacsServers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "node_pacs_servers",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ae_title = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    host = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    port = table.Column<int>(type: "INTEGER", nullable: false),
                    priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 10),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    synced_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_node_pacs_servers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_node_pacs_servers_ae_title",
                table: "node_pacs_servers",
                column: "ae_title");

            migrationBuilder.CreateIndex(
                name: "ix_node_pacs_servers_is_enabled",
                table: "node_pacs_servers",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "ix_node_pacs_servers_priority",
                table: "node_pacs_servers",
                column: "priority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "node_pacs_servers");
        }
    }
}
