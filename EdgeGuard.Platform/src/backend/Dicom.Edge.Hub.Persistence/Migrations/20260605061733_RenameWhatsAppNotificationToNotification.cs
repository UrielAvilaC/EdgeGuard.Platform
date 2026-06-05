using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameWhatsAppNotificationToNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "whatsapp_notifications");

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    study_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    patient_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    normalized_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    template_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    content_sid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    study_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    triggered_by = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    provider_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    to_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    rendered_body = table.Column<string>(type: "text", nullable: true),
                    is_html_body = table.Column<bool>(type: "boolean", nullable: false),
                    attachment_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_channel_status_next_attempt_at",
                table: "notifications",
                columns: new[] { "channel", "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_created_at",
                table: "notifications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_provider_name",
                table: "notifications",
                column: "provider_name");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_status",
                table: "notifications",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_status_created_at",
                table: "notifications",
                columns: new[] { "status", "created_at" },
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_study_id",
                table: "notifications",
                column: "study_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_study_status",
                table: "notifications",
                column: "study_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.CreateTable(
                name: "whatsapp_notifications",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    content_sid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    normalized_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    patient_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    provider_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    study_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    study_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    template_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    triggered_by = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_whatsapp_notifications", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_notifications_created_at",
                table: "whatsapp_notifications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_notifications_provider_name",
                table: "whatsapp_notifications",
                column: "provider_name");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_notifications_status",
                table: "whatsapp_notifications",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_notifications_status_created_at",
                table: "whatsapp_notifications",
                columns: new[] { "status", "created_at" },
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_notifications_study_id",
                table: "whatsapp_notifications",
                column: "study_id");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_notifications_study_status",
                table: "whatsapp_notifications",
                column: "study_status");
        }
    }
}
