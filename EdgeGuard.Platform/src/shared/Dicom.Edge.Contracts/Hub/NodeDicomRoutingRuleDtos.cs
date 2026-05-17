using System.ComponentModel.DataAnnotations;

namespace Dicom.Edge.Contracts.Hub;

// ── Hub-managed DICOM routing rules (per node) ───────────────────────────────

public sealed class NodeDicomRoutingRuleDto
{
    public string   Id                 { get; init; } = default!;
    public string   NodeId             { get; init; } = default!;
    public string   Name               { get; init; } = default!;
    public int      Priority           { get; init; }
    public bool     IsEnabled          { get; init; }
    public string?  MatchModality      { get; init; }
    public string?  MatchSourceAeTitle { get; init; }
    public string?  MatchInstitution   { get; init; }
    public string?  MatchStudyDesc     { get; init; }
    public int?     MinInstanceCount   { get; init; }
    public int?     MaxInstanceCount   { get; init; }
    public string   DestinationAeTitle { get; init; } = default!;
    public bool     SendToPacs         { get; init; }
    public bool     SendToHub          { get; init; }
    public bool     AnonymizeBeforeSend{ get; init; }
    public int      MatchCount         { get; init; }
    public DateTime? LastMatchedAt     { get; init; }
    public DateTime CreatedAt          { get; init; }
    public DateTime? UpdatedAt         { get; init; }
}

public sealed class CreateNodeDicomRoutingRuleRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Name               { get; init; }

    [Range(1, 10000)]
    public int    Priority                    { get; init; } = 100;

    [StringLength(10)]  public string? MatchModality      { get; init; }
    [StringLength(16)]  public string? MatchSourceAeTitle { get; init; }
    [StringLength(100)] public string? MatchInstitution   { get; init; }
    [StringLength(200)] public string? MatchStudyDesc     { get; init; }

    [Range(0, int.MaxValue)] public int? MinInstanceCount { get; init; }
    [Range(0, int.MaxValue)] public int? MaxInstanceCount { get; init; }

    [Required, StringLength(16, MinimumLength = 1)]
    public required string DestinationAeTitle { get; init; }

    public bool SendToPacs          { get; init; } = true;
    public bool SendToHub           { get; init; } = true;
    public bool AnonymizeBeforeSend { get; init; }
}

public sealed class UpdateNodeDicomRoutingRuleRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Name               { get; init; }

    [Range(1, 10000)]
    public int    Priority                    { get; init; } = 100;

    [StringLength(10)]  public string? MatchModality      { get; init; }
    [StringLength(16)]  public string? MatchSourceAeTitle { get; init; }
    [StringLength(100)] public string? MatchInstitution   { get; init; }
    [StringLength(200)] public string? MatchStudyDesc     { get; init; }

    [Range(0, int.MaxValue)] public int? MinInstanceCount { get; init; }
    [Range(0, int.MaxValue)] public int? MaxInstanceCount { get; init; }

    public bool SendToPacs          { get; init; }
    public bool SendToHub           { get; init; }
    public bool AnonymizeBeforeSend { get; init; }
}

// ── Node sync payload (Hub → Node) ──────────────────────────────────────────

public sealed class DicomRoutingRulesSyncRequest
{
    public required string NodeId      { get; init; }
    public DateTime SyncedAtUtc        { get; init; } = DateTime.UtcNow;
    public required IReadOnlyList<DicomRoutingRuleSyncEntry> Rules { get; init; }
}

public sealed class DicomRoutingRuleSyncEntry
{
    public required string Id                 { get; init; }
    public required string Name               { get; init; }
    public int      Priority                  { get; init; }
    public bool     IsEnabled                 { get; init; }
    public string?  MatchModality             { get; init; }
    public string?  MatchSourceAeTitle        { get; init; }
    public string?  MatchInstitution          { get; init; }
    public string?  MatchStudyDescContains    { get; init; }
    public int?     MinInstanceCount          { get; init; }
    public int?     MaxInstanceCount          { get; init; }
    public required string DestinationAeTitle { get; init; }
    public bool     SendToPacs                { get; init; }
    public bool     SendToHub                 { get; init; }
    public bool     AnonymizeBeforeSending    { get; init; }
}

public sealed class DicomRoutingRulesSyncResponse
{
    public bool Accepted      { get; init; }
    public int  AppliedCount  { get; init; }
    public int  RemovedCount  { get; init; }
    public DateTime AppliedAt { get; init; }
    public string? Error      { get; init; }
}
