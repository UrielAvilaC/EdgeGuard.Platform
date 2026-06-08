using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Outbox;

/// <summary>
/// Catalog entry describing one kind of outbox message (a "topic"). Both the node-sync
/// outbox (<c>node_outbox_messages</c>) and the notification outbox (<c>notifications</c>)
/// reference a topic by FK, so the monitoring UI can group/filter every outbox action by a
/// single, well-known catalog. Seeded at startup; rarely changes.
/// The <see cref="Entity{TId}.Id"/> is the natural topic key (e.g. <c>node.config</c>).
/// </summary>
public sealed class OutboxTopic : Entity<string>
{
    /// <summary>High-level family: <c>NodeSync</c> or <c>Notification</c> (see <see cref="OutboxTopicCatalog"/>).</summary>
    public string Category { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool Enabled { get; private set; }

    /// <summary>Default retry ceiling a dispatcher applies to messages of this topic.</summary>
    public int DefaultMaxAttempts { get; private set; }

    private OutboxTopic() { }

    public static OutboxTopic Create(
        string key,
        string category,
        string displayName,
        string? description = null,
        bool enabled = true,
        int defaultMaxAttempts = 5)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Topic key cannot be empty.", nameof(key));
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Topic category cannot be empty.", nameof(category));

        return new OutboxTopic
        {
            Id = key.Trim().ToLowerInvariant(),
            Category = category.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim(),
            Enabled = enabled,
            DefaultMaxAttempts = defaultMaxAttempts,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Enable() { Enabled = true; UpdatedAt = DateTime.UtcNow; }
    public void Disable() { Enabled = false; UpdatedAt = DateTime.UtcNow; }
}
