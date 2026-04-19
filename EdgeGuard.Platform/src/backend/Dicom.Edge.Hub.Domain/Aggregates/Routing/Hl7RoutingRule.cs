using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Routing;

/// <summary>
/// Hub-level routing rule that determines which Edge Node receives a given HL7 message.
/// Rules are evaluated by Priority (ascending). First match wins.
/// </summary>
public sealed class Hl7RoutingRule : AggregateRoot<string>
{
    public string Name { get; private set; } = default!;
    public int Priority { get; private set; }
    public bool IsEnabled { get; private set; }

    // ── Match conditions (null = any) ─────────────────────────────────────────
    public string? MatchMessageType { get; private set; }
    public string? MatchTriggerEvent { get; private set; }
    public string? MatchSendingFacility { get; private set; }
    public string? MatchSendingApplication { get; private set; }

    // ── Target ────────────────────────────────────────────────────────────────
    public string TargetNodeId { get; private set; } = default!;

    // ── Metrics ───────────────────────────────────────────────────────────────
    public int MatchCount { get; private set; }
    public DateTime? LastMatchedAt { get; private set; }

    private Hl7RoutingRule() { }

    public static Hl7RoutingRule Create(
        string name,
        string targetNodeId,
        int priority = 100,
        string? matchMessageType = null,
        string? matchTriggerEvent = null,
        string? matchSendingFacility = null,
        string? matchSendingApplication = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Rule name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(targetNodeId))
            throw new ArgumentException("Target node ID cannot be empty.", nameof(targetNodeId));

        return new Hl7RoutingRule
        {
            Id = IdGenerator.NewId(),
            Name = name.Trim(),
            TargetNodeId = targetNodeId,
            Priority = priority,
            IsEnabled = true,
            MatchMessageType = matchMessageType?.Trim().ToUpperInvariant(),
            MatchTriggerEvent = matchTriggerEvent?.Trim().ToUpperInvariant(),
            MatchSendingFacility = matchSendingFacility?.Trim(),
            MatchSendingApplication = matchSendingApplication?.Trim(),
        };
    }

    public bool Matches(string? messageType, string? triggerEvent, string? sendingFacility, string? sendingApplication)
    {
        if (!IsEnabled) return false;

        if (MatchMessageType is not null &&
            !string.Equals(MatchMessageType, messageType, StringComparison.OrdinalIgnoreCase))
            return false;

        if (MatchTriggerEvent is not null &&
            !string.Equals(MatchTriggerEvent, triggerEvent, StringComparison.OrdinalIgnoreCase))
            return false;

        if (MatchSendingFacility is not null &&
            !string.Equals(MatchSendingFacility, sendingFacility, StringComparison.OrdinalIgnoreCase))
            return false;

        if (MatchSendingApplication is not null &&
            !string.Equals(MatchSendingApplication, sendingApplication, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public void RecordMatch()
    {
        MatchCount++;
        LastMatchedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable() { IsEnabled = true; UpdatedAt = DateTime.UtcNow; }
    public void Disable() { IsEnabled = false; UpdatedAt = DateTime.UtcNow; }

    public void UpdatePriority(int priority) { Priority = priority; UpdatedAt = DateTime.UtcNow; }

    public void Update(
        string name,
        string targetNodeId,
        int priority,
        string? matchMessageType,
        string? matchTriggerEvent,
        string? matchSendingFacility,
        string? matchSendingApplication)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Rule name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(targetNodeId))
            throw new ArgumentException("Target node ID cannot be empty.", nameof(targetNodeId));

        Name = name.Trim();
        TargetNodeId = targetNodeId;
        Priority = priority;
        MatchMessageType = matchMessageType?.Trim().ToUpperInvariant();
        MatchTriggerEvent = matchTriggerEvent?.Trim().ToUpperInvariant();
        MatchSendingFacility = matchSendingFacility?.Trim();
        MatchSendingApplication = matchSendingApplication?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
