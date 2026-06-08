using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxActivityView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE VIEW vw_outbox_activity AS
SELECT n.id, t.category, n.topic_id, t.display_name AS topic_name,
       COALESCE(n.to_email, n.normalized_phone, n.phone_number) AS target,
       n.status, n.attempts, n.next_attempt_at, n.last_error, n.created_at,
       n.sent_at AS processed_at
FROM notifications n
JOIN outbox_topics t ON t.id = n.topic_id
UNION ALL
SELECT m.id, t.category, m.topic_id, t.display_name AS topic_name,
       m.node_id AS target,
       m.status, m.attempts, m.next_attempt_at, m.last_error, m.created_at,
       m.sent_at AS processed_at
FROM node_outbox_messages m
JOIN outbox_topics t ON t.id = m.topic_id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_outbox_activity;");
        }
    }
}
