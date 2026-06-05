using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameAutoSendRuleAddChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_whatsapp_auto_send_rules_study_status",
                table: "whatsapp_auto_send_rules");

            migrationBuilder.AddColumn<bool>(
                name: "attach_pdf",
                table: "whatsapp_auto_send_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "channel",
                table: "whatsapp_auto_send_rules",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "WhatsApp");

            migrationBuilder.AddColumn<bool>(
                name: "include_qr",
                table: "whatsapp_auto_send_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "image_link",
                table: "notifications",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "notification_templates",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_templates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_auto_send_rules_study_status_channel",
                table: "whatsapp_auto_send_rules",
                columns: new[] { "study_status", "channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_templates_is_active",
                table: "notification_templates",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_notification_templates_name",
                table: "notification_templates",
                column: "name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_templates");

            migrationBuilder.DropIndex(
                name: "ix_whatsapp_auto_send_rules_study_status_channel",
                table: "whatsapp_auto_send_rules");

            migrationBuilder.DropColumn(
                name: "attach_pdf",
                table: "whatsapp_auto_send_rules");

            migrationBuilder.DropColumn(
                name: "channel",
                table: "whatsapp_auto_send_rules");

            migrationBuilder.DropColumn(
                name: "include_qr",
                table: "whatsapp_auto_send_rules");

            migrationBuilder.DropColumn(
                name: "image_link",
                table: "notifications");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_auto_send_rules_study_status",
                table: "whatsapp_auto_send_rules",
                column: "study_status",
                unique: true);
        }
    }
}
