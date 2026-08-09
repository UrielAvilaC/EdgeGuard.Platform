using System.ComponentModel.DataAnnotations;

namespace Dicom.Edge.Contracts.Hub;

// ── Edge (Node-facing) ───────────────────────────────────────────────────────

public sealed class NodeRegistrationRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(16, MinimumLength = 1)]
    public required string AeTitle { get; init; }

    [Required, StringLength(64, MinimumLength = 1)]
    public required string IpAddress { get; init; }

    [Range(1, 65535)]
    public required int Port { get; init; }

    [StringLength(512)]
    public string? ApiEndpoint { get; init; }

    [StringLength(200)]
    public string? Location { get; init; }

    [StringLength(200)]
    public string? FacilityName { get; init; }

    [StringLength(32)]
    public string? Version { get; init; }
}

public sealed class NodeRegistrationResponse
{
    public required string NodeId { get; init; }
    public required bool Accepted { get; init; }
    public string? Message { get; init; }

    /// <summary>
    /// API key for subsequent M2M calls. Only returned on first registration.
    /// The node must store this securely — it cannot be retrieved again.
    /// </summary>
    public string? ApiKey { get; init; }
}

public sealed class NodeHeartbeatRequest
{
    [Required, StringLength(36, MinimumLength = 1)]
    public required string NodeId { get; init; }

    [Range(0, long.MaxValue)]
    public long? AvailableStorageMb { get; init; }

    [Range(0, int.MaxValue)]
    public int? TotalStudiesReceived { get; init; }

    [Range(0, int.MaxValue)]
    public int? TotalStudiesSent { get; init; }

    [Range(0, int.MaxValue)]
    public int? ErrorsLast24Hours { get; init; }
}

public sealed class NodeTelemetryRequest
{
    [Required, StringLength(36, MinimumLength = 1)]
    public required string NodeId { get; init; }

    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd   { get; init; }

    // ── Associations (from dicom_associations) ────────────────────────────
    [Range(0, int.MaxValue)] public int TotalAssociations    { get; init; }
    [Range(0, int.MaxValue)] public int AcceptedAssociations { get; init; }
    [Range(0, int.MaxValue)] public int RejectedAssociations { get; init; }
    [Range(0, int.MaxValue)] public int AbortedAssociations  { get; init; }
    [Range(0, int.MaxValue)] public int TotalImagesReceived  { get; init; }

    // ── Study metrics (from study_metrics) ───────────────────────────────
    [Range(0, int.MaxValue)]  public int    CompletedStudies           { get; init; }
    [Range(0, long.MaxValue)] public long   TotalBytesReceived         { get; init; }
    public double? AverageReceptionDurationMs { get; init; }
    public double? AverageThroughputMbps      { get; init; }
}

public sealed class StudyNotifyRequest
{
    [Required, StringLength(36, MinimumLength = 1)]
    public required string NodeId { get; init; }

    [Required, StringLength(64, MinimumLength = 1)]
    public required string StudyInstanceUid { get; init; }

    [StringLength(64)]
    public string? PatientId { get; init; }

    [StringLength(256)]
    public string? PatientName { get; init; }

    [StringLength(64)]
    public string? AccessionNumber { get; init; }

    [Range(0, int.MaxValue)]
    public int InstanceCount { get; init; }

    [Range(0, long.MaxValue)]
    public long TotalSizeBytes { get; init; }

    public DateTime? StudyDate { get; init; }

    [StringLength(512)]
    public string? StudyDescription { get; init; }

    [Range(0, int.MaxValue)]
    public int SeriesCount { get; init; }
}

/// <summary>
/// Sent by the Edge Node on each received C-STORE
/// Used for incremental UI updates and HL7 merge reconciliation.
/// </summary>
public sealed class StudyProgressNotifyRequest
{
    [Required, StringLength(36, MinimumLength = 1)]
    public required string NodeId { get; init; }

    [Required, StringLength(64, MinimumLength = 1)]
    public required string StudyInstanceUid { get; init; }

    [StringLength(64)]
    public string? AccessionNumber { get; init; }

    [StringLength(64)]
    public string? PatientId { get; init; }

    [StringLength(256)]
    public string? PatientName { get; init; }

    [Range(0, int.MaxValue)]
    public int InstanceCount { get; init; }

    [Range(0, long.MaxValue)]
    public long TotalSizeBytes { get; init; }

    public DateTime? StudyDate { get; init; }

    [StringLength(512)]
    public string? StudyDescription { get; init; }

    [Range(0, int.MaxValue)]
    public int SeriesCount { get; init; }
}

/// <summary>
/// Sent by the Edge Node to report the PACS-send phase of a study so the Hub can
/// advance its status to <c>Sending</c> (Enviando a PACS) / <c>SentToPacs</c> (Enviado a PACS) / <c>Failed</c>.
/// These states are surfaced in the study-detail timeline.
/// </summary>
public sealed class StudyPacsStatusNotifyRequest
{
    [Required, StringLength(36, MinimumLength = 1)]
    public required string NodeId { get; init; }

    [Required, StringLength(64, MinimumLength = 1)]
    public required string StudyInstanceUid { get; init; }

    /// <summary>One of <c>Sending</c>, <c>SentToPacs</c> or <c>Failed</c>.</summary>
    [Required, StringLength(32, MinimumLength = 1)]
    public required string Status { get; init; }

    /// <summary>AE Title of the destination PACS, when known.</summary>
    [StringLength(16)]
    public string? TargetPacsAeTitle { get; init; }

    /// <summary>Error message when <see cref="Status"/> is <c>Failed</c>.</summary>
    [StringLength(500)]
    public string? Error { get; init; }
}

public sealed class NodeHealthReportRequest
{
    [Required, StringLength(36, MinimumLength = 1)]
    public required string NodeId { get; init; }

    [Range(0, long.MaxValue)]
    public long? AvailableStorageMb { get; init; }

    [Range(0, 100)]
    public double? CpuPercent { get; init; }

    [Range(0, 100)]
    public double? MemoryPercent { get; init; }

    [Range(0, int.MaxValue)]
    public int? QueueDepth { get; init; }
}

// ── Nodes CRUD ───────────────────────────────────────────────────────────────

public sealed class CreateNodeRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(16, MinimumLength = 1)]
    public required string AeTitle { get; init; }

    [Required, StringLength(64, MinimumLength = 1)]
    public required string IpAddress { get; init; }

    [Range(1, 65535)]
    public required int Port { get; init; }

    [StringLength(512)]
    public string? ApiEndpoint { get; init; }

    [StringLength(200)]
    public string? Location { get; init; }

    [StringLength(200)]
    public string? FacilityName { get; init; }

    [Range(10, 3600)]
    public int HealthCheckIntervalSeconds { get; init; } = 60;
}

// ── PACS Servers ─────────────────────────────────────────────────────────────

public sealed class CreatePacsServerRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(16, MinimumLength = 1)]
    public required string AeTitle { get; init; }

    [Required, StringLength(256, MinimumLength = 1)]
    public required string HostName { get; init; }

    [Range(1, 65535)]
    public required int Port { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    public bool IsGlobal { get; init; }

    [Range(1, 100)]
    public int MaxConcurrentAssociations { get; init; } = 10;

    [Range(5, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}

// ── Node PACS Assignments ─────────────────────────────────────────────────────

public sealed class AssignPacsRequest
{
    [Range(5, 86400)]
    public int CEchoIntervalSeconds { get; init; } = 300;
}

// ── Routing Rules ────────────────────────────────────────────────────────────

public sealed class CreateRoutingRuleRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(36, MinimumLength = 1)]
    public required string TargetNodeId { get; init; }

    [Range(1, 10000)]
    public int Priority { get; init; } = 100;

    [StringLength(10)]
    public string? MatchMessageType { get; init; }

    [StringLength(10)]
    public string? MatchTriggerEvent { get; init; }

    [StringLength(100)]
    public string? MatchSendingFacility { get; init; }

    [StringLength(100)]
    public string? MatchSendingApplication { get; init; }
}

public sealed class UpdatePriorityRequest
{
    [Range(1, 10000)]
    public required int Priority { get; init; }
}

// ── System Settings ──────────────────────────────────────────────────────────

public sealed class UpdateSettingRequest
{
    [Required]
    public required string Value { get; init; }
}

// ── Update Node Metadata ──────────────────────────────────────────────────────

public sealed class UpdateNodeRequest
{
    [StringLength(200)]
    public string? Location { get; init; }

    [StringLength(200)]
    public string? FacilityName { get; init; }

    [StringLength(64)]
    public string? TimeZone { get; init; }

    [Range(10, 3600)]
    public int? HealthCheckIntervalSeconds { get; init; }

    [Range(0, long.MaxValue)]
    public long? MaxStorageMb { get; init; }
}

// ── Update PACS Server ───────────────────────────────────────────────────────

public sealed class UpdatePacsServerRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(256, MinimumLength = 1)]
    public required string HostName { get; init; }

    [Range(1, 65535)]
    public required int Port { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    [Range(1, 100)]
    public int MaxConcurrentAssociations { get; init; } = 10;

    [Range(5, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}

// ── Update Routing Rule ──────────────────────────────────────────────────────

public sealed class UpdateRoutingRuleRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(36, MinimumLength = 1)]
    public required string TargetNodeId { get; init; }

    [Range(1, 10000)]
    public int Priority { get; init; } = 100;

    [StringLength(10)]
    public string? MatchMessageType { get; init; }

    [StringLength(10)]
    public string? MatchTriggerEvent { get; init; }

    [StringLength(100)]
    public string? MatchSendingFacility { get; init; }

    [StringLength(100)]
    public string? MatchSendingApplication { get; init; }
}

// ── Update Patient Contact ───────────────────────────────────────────────────

public sealed class UpdatePatientRequest
{
    [StringLength(256)]
    public string? PatientName { get; init; }

    public DateOnly? BirthDate { get; init; }

    [StringLength(10)]
    public string? Sex { get; init; }

    [StringLength(20)]
    public string? PhoneNumber { get; init; }

    [StringLength(256)]
    public string? Email { get; init; }
}

// ── Update Study ─────────────────────────────────────────────────────────────

public sealed class UpdateStudyRequest
{
    [StringLength(256)]
    public string? StudyDescription { get; init; }

    [StringLength(256)]
    public string? ReferringPhysician { get; init; }

    [StringLength(64)]
    public string? AccessionNumber { get; init; }

    [Range(0, 10)]
    public int? Priority { get; init; }

    public bool? IsUrgent { get; init; }
}

// ── PACS C-ECHO report (Node → Hub) ─────────────────────────────────────────

/// <summary>
/// Node reports the latest PACS C-ECHO results to the Hub so the SPA can display connectivity status.
/// </summary>
public sealed class NodePacsEchoReportRequest
{
    [Required, StringLength(36, MinimumLength = 1)]
    public required string NodeId { get; init; }

    public required IReadOnlyList<PacsEchoDestinationResult> Results { get; init; }

    public DateTime ReportedAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Per-destination result inside a <see cref="NodePacsEchoReportRequest"/>.
/// </summary>
public sealed class PacsEchoDestinationResult
{
    [Required, StringLength(16, MinimumLength = 1)]
    public required string AeTitle { get; init; }

    [Required]
    public required string Host { get; init; }

    public required int Port { get; init; }

    public required bool Success { get; init; }

    public double? LatencyMs { get; init; }

    /// <summary>Human-readable exception or DICOM status message.</summary>
    public string? Error { get; init; }

    /// <summary>
    /// Structured DICOM rejection reason, e.g. "CalledAENotRecognized".
    /// Null when successful or the failure is non-DICOM.
    /// </summary>
    public string? ErrorReason { get; init; }

    public required DateTime CheckedAtUtc { get; init; }
}

/// <summary>
/// Node reports recent equipment activity (passive presence) so the Hub can show
/// per-equipment last-seen / online status. Only equipment seen since the last report
/// are included (delta).
/// </summary>
public sealed class NodeEquipmentStatusReportRequest
{
    [Required, StringLength(36, MinimumLength = 1)]
    public required string NodeId { get; init; }

    public DateTime ReportedAtUtc { get; init; } = DateTime.UtcNow;

    public required IReadOnlyList<EquipmentStatusEntry> Equipment { get; init; }
}

/// <summary>
/// Per-equipment presence entry inside a <see cref="NodeEquipmentStatusReportRequest"/>.
/// </summary>
public sealed class EquipmentStatusEntry
{
    [Required, StringLength(16, MinimumLength = 1)]
    public required string AeTitle { get; init; }

    /// <summary>UTC timestamp of the equipment's most recent accepted association.</summary>
    public required DateTime LastSeenUtc { get; init; }
}

// ── Update Study Status (manual) ─────────────────────────────────────────────

public sealed class UpdateStudyStatusRequest
{
    [Required]
    public required string Status { get; init; }

    [StringLength(500)]
    public string? Reason { get; init; }
}

// ── Manual study resend ───────────────────────────────────────────────────────

/// <summary>POST /api/studies/{id}/requeue — manual resend of a Failed study to chosen PACS.</summary>
public sealed class RequeueStudyRequest
{
    [Required, MinLength(1)]
    public required IReadOnlyList<string> PacsIds { get; init; }
}

// ── Node Configuration ───────────────────────────────────────────────────────

public sealed class UpdateNodeSettingRequest
{
    [Required]
    public required string Value { get; init; }
}

/// <summary>Saves multiple node settings in a single Hub call.</summary>
public sealed class BatchUpdateNodeSettingsRequest
{
    [Required, MinLength(1)]
    public required List<NodeSettingUpdateItem> Settings { get; init; }
}

public sealed record NodeSettingUpdateItem(
    [Required] string Key,
    [Required] string Value);

/// <summary>Result of a batch update operation.</summary>
public sealed class BatchUpdateNodeSettingsResponse
{
    public int Updated { get; init; }
    public int NotFound { get; init; }
    public List<string> FailedKeys { get; init; } = [];
}

// ── Node Bootstrap Tokens ─────────────────────────────────────────────────────

public sealed class CreateBootstrapTokenRequest
{
    [StringLength(200)]
    public string? Note { get; init; }

    /// <summary>Token validity in hours. Defaults to 24.</summary>
    [Range(1, 720)]
    public int ExpiresInHours { get; init; } = 24;
}

public sealed class BootstrapTokenResponse
{
    /// <summary>Raw token — shown once. Store it securely.</summary>
    public required string Token { get; init; }
    public required string TokenId { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public string? Note { get; init; }
}
