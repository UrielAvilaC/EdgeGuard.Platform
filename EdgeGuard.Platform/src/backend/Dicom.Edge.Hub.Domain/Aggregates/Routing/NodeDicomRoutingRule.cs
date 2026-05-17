using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Routing;

/// <summary>
/// Hub-managed DICOM routing rule scoped to a single Edge Node.
/// Determines which PACS destination receives a completed DICOM study,
/// evaluated by the node's RuleBasedStudyRouter in priority order.
/// All null conditions match any value (wildcard).
/// </summary>
public sealed class NodeDicomRoutingRule : AggregateRoot<string>
{
    public string NodeId              { get; private set; } = default!;
    public string Name                { get; private set; } = default!;
    public int    Priority            { get; private set; }
    public bool   IsEnabled           { get; private set; }

    // ── Match conditions (null = any) ──────────────────────────────────────────
    public string? MatchModality      { get; private set; }
    public string? MatchSourceAeTitle { get; private set; }
    public string? MatchInstitution   { get; private set; }
    public string? MatchStudyDesc     { get; private set; }
    public int?    MinInstanceCount   { get; private set; }
    public int?    MaxInstanceCount   { get; private set; }

    // ── Destination ────────────────────────────────────────────────────────────
    public string DestinationAeTitle  { get; private set; } = default!;

    // ── Actions ────────────────────────────────────────────────────────────────
    public bool   SendToPacs          { get; private set; }
    public bool   SendToHub           { get; private set; }
    public bool   AnonymizeBeforeSend { get; private set; }

    // ── Analytics ──────────────────────────────────────────────────────────────
    public int      MatchCount        { get; private set; }
    public DateTime? LastMatchedAt    { get; private set; }

    private NodeDicomRoutingRule() { }

    public static NodeDicomRoutingRule Create(
        string  nodeId,
        string  name,
        string  destinationAeTitle,
        int     priority            = 100,
        string? matchModality       = null,
        string? matchSourceAeTitle  = null,
        string? matchInstitution    = null,
        string? matchStudyDesc      = null,
        int?    minInstanceCount    = null,
        int?    maxInstanceCount    = null,
        bool    sendToPacs          = true,
        bool    sendToHub           = true,
        bool    anonymizeBeforeSend = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationAeTitle);

        return new NodeDicomRoutingRule
        {
            Id                 = IdGenerator.NewId(),
            NodeId             = nodeId,
            Name               = name.Trim(),
            Priority           = priority,
            IsEnabled          = true,
            MatchModality      = matchModality?.Trim().ToUpperInvariant(),
            MatchSourceAeTitle = matchSourceAeTitle?.Trim(),
            MatchInstitution   = matchInstitution?.Trim(),
            MatchStudyDesc     = matchStudyDesc?.Trim(),
            MinInstanceCount   = minInstanceCount,
            MaxInstanceCount   = maxInstanceCount,
            DestinationAeTitle = destinationAeTitle.Trim(),
            SendToPacs         = sendToPacs,
            SendToHub          = sendToHub,
            AnonymizeBeforeSend = anonymizeBeforeSend,
        };
    }

    public void Update(
        string  name,
        int     priority,
        string? matchModality,
        string? matchSourceAeTitle,
        string? matchInstitution,
        string? matchStudyDesc,
        int?    minInstanceCount,
        int?    maxInstanceCount,
        bool    sendToPacs,
        bool    sendToHub,
        bool    anonymizeBeforeSend)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name               = name.Trim();
        Priority           = priority;
        MatchModality      = matchModality?.Trim().ToUpperInvariant();
        MatchSourceAeTitle = matchSourceAeTitle?.Trim();
        MatchInstitution   = matchInstitution?.Trim();
        MatchStudyDesc     = matchStudyDesc?.Trim();
        MinInstanceCount   = minInstanceCount;
        MaxInstanceCount   = maxInstanceCount;
        SendToPacs         = sendToPacs;
        SendToHub          = sendToHub;
        AnonymizeBeforeSend = anonymizeBeforeSend;
        UpdatedAt          = DateTime.UtcNow;
    }

    public void Enable()            { IsEnabled = true;  UpdatedAt = DateTime.UtcNow; }
    public void Disable()           { IsEnabled = false; UpdatedAt = DateTime.UtcNow; }
    public void UpdatePriority(int priority) { Priority = priority; UpdatedAt = DateTime.UtcNow; }
}
