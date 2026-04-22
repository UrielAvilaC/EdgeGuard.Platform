using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeBootstrapTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "node_bootstrap_tokens",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_consumed = table.Column<bool>(type: "boolean", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    consumed_by_node_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_by_user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_bootstrap_tokens", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_node_bootstrap_tokens_expires_at",
                table: "node_bootstrap_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_node_bootstrap_tokens_is_consumed",
                table: "node_bootstrap_tokens",
                column: "is_consumed");

            migrationBuilder.CreateIndex(
                name: "ix_node_bootstrap_tokens_token_hash",
                table: "node_bootstrap_tokens",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "node_bootstrap_tokens");
        }
    }
}
