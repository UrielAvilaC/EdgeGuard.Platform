using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationTopicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Add the column nullable first so existing rows can be backfilled.
            migrationBuilder.AddColumn<string>(
                name: "topic_id",
                table: "notifications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            // 2) Backfill topic_id from the channel (stored as 'Email' / 'WhatsApp').
            migrationBuilder.Sql(
                "UPDATE notifications SET topic_id = CASE channel " +
                "WHEN 'Email' THEN 'notification.email' ELSE 'notification.whatsapp' END;");

            // 3) Ensure the referenced topics exist before the FK is enforced. Migrations run
            //    before the runtime seeder (OutboxTopicSeed), so seed them here idempotently.
            migrationBuilder.Sql(
                "INSERT INTO outbox_topics (id, category, display_name, description, enabled, default_max_attempts, created_at, updated_at) VALUES " +
                "('node.config','NodeSync','Node Configuration','Full configuration snapshot pushed to a node',true,5,now(),now())," +
                "('node.rules','NodeSync','DICOM Routing Rules','Per-node DICOM routing rules pushed to a node',true,5,now(),now())," +
                "('node.pacs','NodeSync','PACS Destinations','PACS destination assignments pushed to a node',true,5,now(),now())," +
                "('node.equipment','NodeSync','Equipment Catalog','Equipment catalog with modality codes pushed to a node',true,5,now(),now())," +
                "('notification.email','Notification','Email','Patient results delivery over Email',true,5,now(),now())," +
                "('notification.whatsapp','Notification','WhatsApp','Patient results delivery over WhatsApp',true,5,now(),now()) " +
                "ON CONFLICT (id) DO NOTHING;");

            // 4) Now enforce NOT NULL.
            migrationBuilder.AlterColumn<string>(
                name: "topic_id",
                table: "notifications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_topic_id",
                table: "notifications",
                column: "topic_id");

            migrationBuilder.AddForeignKey(
                name: "fk_notifications_outbox_topics_topic_id",
                table: "notifications",
                column: "topic_id",
                principalTable: "outbox_topics",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_notifications_outbox_topics_topic_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_notifications_topic_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "topic_id",
                table: "notifications");
        }
    }
}
