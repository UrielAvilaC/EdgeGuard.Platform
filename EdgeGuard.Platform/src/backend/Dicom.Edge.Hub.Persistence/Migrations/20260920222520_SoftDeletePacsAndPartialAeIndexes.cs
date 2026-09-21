using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeletePacsAndPartialAeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_pacs_servers_ae_title",
                table: "pacs_servers");

            migrationBuilder.DropIndex(
                name: "ix_nodes_ae_title",
                table: "nodes");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "pacs_servers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "pacs_servers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_pacs_servers_ae_title",
                table: "pacs_servers",
                column: "ae_title",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_servers_is_deleted",
                table: "pacs_servers",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_nodes_ae_title",
                table: "nodes",
                column: "ae_title",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_pacs_servers_ae_title",
                table: "pacs_servers");

            migrationBuilder.DropIndex(
                name: "ix_pacs_servers_is_deleted",
                table: "pacs_servers");

            migrationBuilder.DropIndex(
                name: "ix_nodes_ae_title",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "pacs_servers");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "pacs_servers");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_servers_ae_title",
                table: "pacs_servers",
                column: "ae_title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_nodes_ae_title",
                table: "nodes",
                column: "ae_title",
                unique: true);
        }
    }
}
