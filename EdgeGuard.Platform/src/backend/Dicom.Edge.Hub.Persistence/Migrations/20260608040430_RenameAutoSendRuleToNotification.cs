using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameAutoSendRuleToNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "whatsapp_auto_send_rules");

            migrationBuilder.CreateTable(
                name: "notification_auto_send_rules",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    study_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    template_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "WhatsApp"),
                    attach_pdf = table.Column<bool>(type: "boolean", nullable: false),
                    include_qr = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_auto_send_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_auto_send_rules_study_status_channel",
                table: "notification_auto_send_rules",
                columns: new[] { "study_status", "channel" },
                unique: true);

            // Rename the retention setting key, preserving any operator-customized value.
            migrationBuilder.Sql(
                "UPDATE system_settings SET key = 'retention.notification_days' " +
                "WHERE key = 'retention.whatsapp_notification_days';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_auto_send_rules");

            migrationBuilder.CreateTable(
                name: "whatsapp_auto_send_rules",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    attach_pdf = table.Column<bool>(type: "boolean", nullable: false),
                    channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "WhatsApp"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    include_qr = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    study_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    template_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_whatsapp_auto_send_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_auto_send_rules_study_status_channel",
                table: "whatsapp_auto_send_rules",
                columns: new[] { "study_status", "channel" },
                unique: true);

            // Revert the retention setting key rename.
            migrationBuilder.Sql(
                "UPDATE system_settings SET key = 'retention.whatsapp_notification_days' " +
                "WHERE key = 'retention.notification_days';");
        }
    }
}
