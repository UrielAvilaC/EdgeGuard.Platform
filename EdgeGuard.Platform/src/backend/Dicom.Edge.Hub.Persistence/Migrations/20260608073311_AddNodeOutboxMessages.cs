using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeOutboxMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "node_outbox_messages",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    topic_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    node_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_outbox_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_node_outbox_messages_outbox_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "outbox_topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_node_outbox_messages_created_at",
                table: "node_outbox_messages",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_node_outbox_messages_node_topic_status",
                table: "node_outbox_messages",
                columns: new[] { "node_id", "topic_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_node_outbox_messages_status_next_attempt",
                table: "node_outbox_messages",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "ix_node_outbox_messages_topic_id",
                table: "node_outbox_messages",
                column: "topic_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "node_outbox_messages");
        }
    }
}
