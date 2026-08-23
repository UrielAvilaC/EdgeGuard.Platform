namespace Dicom.Edge.Contracts.Hub;

// ── Patients ─────────────────────────────────────────────────────────────────

public sealed record PatientDto
{
    public required string Id { get; init; }
    public required string PatientDicomId { get; init; }
    public required string PatientName { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? Sex { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? IssuerOfPatientId { get; init; }
    public string? FacilitySource { get; init; }
    public string? CreatedByNodeId { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

// ── Nodes ────────────────────────────────────────────────────────────────────

public sealed record NodeDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string AeTitle { get; init; }
    public required string IpAddress { get; init; }
    public int Port { get; init; }
    public string? ApiEndpoint { get; init; }
    public string? Location { get; init; }
    public string? FacilityName { get; init; }
    public required string Status { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime? LastHeartbeatAt { get; init; }
    public int HealthCheckIntervalSeconds { get; init; }
    /// <summary>Cuota administrada desde el Hub. null = sin límite, no se pinta barra.</summary>
    public long? StorageLimitMb { get; init; }

    /// <summary>Medición del nodo. null = nunca reportó, que no es lo mismo que cero.</summary>
    public long? StorageDicomMb { get; init; }
    public long? StorageDatabaseMb { get; init; }
    public long? StorageVolumeFreeMb { get; init; }
    public long? StorageVolumeTotalMb { get; init; }
    public DateTime? StorageMeasuredAt { get; init; }

    /// <summary>Límite que el nodo confirmó: si difiere del administrado, el push no ha llegado.</summary>
    public long? StorageLimitAppliedMb { get; init; }

    public string? ConfigAppliedVersion { get; init; }
    public DateTime? ConfigAppliedAt { get; init; }
    public int TotalStudiesReceived { get; init; }
    public int TotalStudiesSent { get; init; }
    public int ErrorsLast24Hours { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<NodePacsAssignmentDto> PacsAssignments { get; init; } = [];
}

public sealed record NodePacsAssignmentDto
{
    public required string PacsId { get; init; }
    public bool IsActive { get; init; }
    public bool InheritedFromHub { get; init; }
}

// ── Telemetry ────────────────────────────────────────────────────────────────

public sealed record NodeTelemetryDto
{
    public required string Id        { get; init; }
    public required string NodeId    { get; init; }
    public DateTime ReportedAt       { get; init; }
    public DateTime PeriodStart      { get; init; }
    public DateTime PeriodEnd        { get; init; }
    public int  TotalAssociations    { get; init; }
    public int  AcceptedAssociations { get; init; }
    public int  RejectedAssociations { get; init; }
    public int  AbortedAssociations  { get; init; }
    public int  TotalImagesReceived  { get; init; }
    public int  CompletedStudies     { get; init; }
    public long TotalBytesReceived   { get; init; }
    public double? AverageReceptionDurationMs { get; init; }
    public double? AverageThroughputMbps      { get; init; }
}

public sealed record NodeTelemetryAckDto
{
    public bool     Acknowledged  { get; init; }
    public DateTime ServerTimeUtc { get; init; }
}

// ── PACS C-ECHO status (Hub-side, per node) ───────────────────────────────────

/// <summary>
/// Hub-side DTO that groups the latest C-ECHO results for a node, returned by
/// <c>GET /api/nodes/{id}/pacs-echo</c>.
/// </summary>
public sealed record NodePacsCEchoStatusDto
{
    public required string NodeId        { get; init; }
    public required DateTime ReportedAtUtc { get; init; }
    public required IReadOnlyList<PacsCEchoDestinationDto> Destinations { get; init; }
    public int TotalChecked   => Destinations.Count;
    public int TotalReachable => Destinations.Count(d => d.Success);
}

public sealed record PacsCEchoDestinationDto
{
    public required string AeTitle      { get; init; }
    public required string Host         { get; init; }
    public required int    Port         { get; init; }
    public required bool   Success      { get; init; }
    public double?         LatencyMs    { get; init; }
    /// <summary>Human-readable error (network, timeout, etc.).</summary>
    public string?         Error        { get; init; }
    /// <summary>Structured DICOM rejection reason, e.g. "CalledAENotRecognized".</summary>
    public string?         ErrorReason  { get; init; }
    public required DateTime CheckedAtUtc { get; init; }
}

// ── Studies ──────────────────────────────────────────────────────────────────

public sealed record StudyDto
{
    public required string Id { get; init; }
    public required string StudyInstanceUid { get; init; }
    public string? AccessionNumber { get; init; }
    public DateTime? StudyDate { get; init; }
    public string? StudyDescription { get; init; }
    public string? ReferringPhysician { get; init; }
    /// <summary>DICOM Patient ID (MRN) as it arrived on the study.</summary>
    public string? PatientId { get; init; }

    /// <summary>Id of the linked patient record in the Hub catalogue, when known.</summary>
    public string? PatientRecordId { get; init; }

    public string? PatientName { get; init; }
    public string? SourceNodeId { get; init; }
    public string? SourceAeTitle { get; init; }
    /// <summary>Clinical lifecycle: Scheduled / Receiving / Completed / WaitingFor… / Finalized.</summary>
    public required string Status { get; init; }

    /// <summary>PACS-send pipeline: NotQueued / Queued / Sending / Sent / Failed. Independent of <see cref="Status"/>.</summary>
    public string PacsStatus { get; init; } = "NotQueued";

    public int InstanceCount { get; init; }
    public int SeriesCount { get; init; }
    public long TotalSizeBytes { get; init; }
    public DateTime? FirstImageReceivedAt { get; init; }
    public DateTime? LastImageReceivedAt { get; init; }
    public int Priority { get; init; }
    public bool IsUrgent { get; init; }
    public string? TargetPacsId { get; init; }
    public DateTime? SentToPacsAt { get; init; }
    public int PacsSendAttempts { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // Results (image links + diagnostic report)
    public string ReportFormat { get; init; } = "None";
    public bool HasReport { get; init; }
    public bool HasImageLinks { get; init; }
    public IReadOnlyList<string> ImageLinks { get; init; } = [];
}

/// <summary>
/// Infrastructure view for a study's detail page: origin node identity + connectivity,
/// target PACS identity + reachability, and PACS-send tracking. Resolved on demand
/// (not part of the study list) so lists avoid per-row node/PACS lookups.
/// </summary>
public sealed record StudyInfrastructureDto
{
    // ── Origin node ──
    public string? SourceNodeId { get; init; }
    public string? SourceNodeName { get; init; }
    public string? SourceAeTitle { get; init; }
    /// <summary>Node status enum name (Online/Offline/Degraded/…), or null when unknown.</summary>
    public string? NodeStatus { get; init; }
    public DateTime? NodeLastHeartbeatAt { get; init; }

    // ── Target PACS ──
    public string? TargetPacsId { get; init; }
    public string? TargetPacsName { get; init; }
    public string? TargetPacsAeTitle { get; init; }
    /// <summary>Last known C-ECHO reachability of the PACS from this node; null when never checked.</summary>
    public bool? PacsReachable { get; init; }
    public DateTime? PacsLastEchoAt { get; init; }

    // ── PACS-send tracking ──
    public DateTime? SentToPacsAt { get; init; }
    public int PacsSendAttempts { get; init; }
    public string? PacsSendLastError { get; init; }
}

/// <summary>Diagnostic report view for a study (sanitized content + links + PDF flag).</summary>
public sealed record ReportDto
{
    public required string StudyId { get; init; }
    public required string Status { get; init; }
    public string ReportFormat { get; init; } = "None";
    /// <summary>Sanitized HTML or plain text report body (null when only a PDF/links).</summary>
    public string? Content { get; init; }
    public bool HasPdf { get; init; }
    public IReadOnlyList<string> ImageLinks { get; init; } = [];
}

// ── PACS Servers ─────────────────────────────────────────────────────────────

public sealed record PacsServerDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string AeTitle { get; init; }
    public required string HostName { get; init; }
    public int Port { get; init; }
    public string? Description { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsGlobal { get; init; }
    public int MaxConcurrentAssociations { get; init; }
    public int TimeoutSeconds { get; init; }
    public DateTime? LastCEchoAt { get; init; }
    public bool LastCEchoSuccess { get; init; }
    public bool IsReachable { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

// ── HL7 Routing Rules ────────────────────────────────────────────────────────

public sealed record Hl7RoutingRuleDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Priority { get; init; }
    public bool IsEnabled { get; init; }
    public string? MatchMessageType { get; init; }
    public string? MatchTriggerEvent { get; init; }
    public string? MatchSendingFacility { get; init; }
    public string? MatchSendingApplication { get; init; }
    public required string TargetNodeId { get; init; }
    public int MatchCount { get; init; }
    public DateTime? LastMatchedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

// ── HL7 Messages (Queue) ────────────────────────────────────────────────────

public sealed record Hl7MessageDto
{
    public required Guid Id { get; init; }
    public required string MessageType { get; init; }
    public string? TriggerEvent { get; init; }
    public string? PatientId { get; init; }
    public string? PatientName { get; init; }
    public string? SendingFacility { get; init; }
    public required string Status { get; init; }
    public required string DispatchStatus { get; init; }
    public string? TargetNodeId { get; init; }
    public string? TargetNodeName { get; init; }
    public int Priority { get; init; }
    public int DispatchAttempts { get; init; }
    public string? DispatchError { get; init; }
    public DateTime ReceivedAt { get; init; }
    public DateTime? ValidatedAt { get; init; }
    public DateTime? RoutedAt { get; init; }
    public DateTime? QueuedAt { get; init; }
    public DateTime? DispatchedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
}

public sealed record Hl7MessageQueuedDto
{
    public required Guid Id { get; init; }
    public required string MessageType { get; init; }
    public string? TriggerEvent { get; init; }
    public string? PatientId { get; init; }
    public string? TargetNodeId { get; init; }
    public string? TargetNodeName { get; init; }
    public int Priority { get; init; }
    public DateTime ReceivedAt { get; init; }
    public DateTime? QueuedAt { get; init; }
}

// ── Node Configuration Profiles ──────────────────────────────────────────────

public sealed record NodeConfigurationProfileDto
{
    public required string NodeId { get; init; }
    public required string SettingKey { get; init; }
    public required string Value { get; init; }
    public required string Category { get; init; }
    public required string DisplayName { get; init; }
    public required string ValueType { get; init; }
    public bool IsOverridden { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

// ── Queue Monitoring ─────────────────────────────────────────────────────────

public sealed record QueueSummaryDto
{
    public int PendingValidation { get; init; }
    public int Validated { get; init; }
    public int Routed { get; init; }
    public int Queued { get; init; }
    public int Dispatching { get; init; }
    public int Delivered { get; init; }
    public int DeliveryFailed { get; init; }
    public int ValidationFailed { get; init; }
    public int TotalInPipeline { get; init; }
}

// ── Edge (Node-facing) Responses ─────────────────────────────────────────────

public sealed record HeartbeatAckDto
{
    public bool Acknowledged { get; init; }
    public DateTime ServerTimeUtc { get; init; }
}

public sealed record StudyNotifyAckDto
{
    public bool Acknowledged { get; init; }
    public required string StudyId { get; init; }
    public DateTime ReceivedAtUtc { get; init; }
}

public sealed record HealthReportAckDto
{
    public bool Acknowledged { get; init; }
}

public sealed record ConfigPushResultDto
{
    public string? AppliedVersion { get; init; }
    public int UpdatedCount { get; init; }
}

// ── Hub Info / Runtime ───────────────────────────────────────────────────────

public sealed record HubInfoDto
{
    public required string Service { get; init; }
    public required string Version { get; init; }
    public DateTime UtcNow { get; init; }
    public string[] Capabilities { get; init; } = [];
}

public sealed record HubRuntimeDto
{
    public required string Service { get; init; }
    public DateTime UtcNow { get; init; }
    public required string Environment { get; init; }
    public required string MachineName { get; init; }
    public required string Framework { get; init; }
}

// ── System Settings ──────────────────────────────────────────────────────────

public sealed record SystemSettingDto(
    string Key,
    string Value,
    string Category,
    string DisplayName,
    string ValueType,
    string? Description,
    bool IsReadOnly);

// ── HL7 Monitoring ───────────────────────────────────────────────────────────

public sealed record Hl7ListenerStatusDto(bool IsRunning, int Port, int ActiveConnections);

public sealed record Hl7MessageSummaryDto(
    Guid Id,
    string MessageType,
    string? SendingApplication,
    string? SendingFacility,
    DateTime ReceivedAt,
    string? ClientEndpoint,
    string Status,
    DateTime? ProcessedAt,
    string? ErrorMessage,
    bool CanReprocess);

public sealed record Hl7MessageDetailDto(
    Guid Id,
    string Content,
    string MessageType,
    string? SendingApplication,
    string? SendingFacility,
    DateTime ReceivedAt,
    string? ClientEndpoint,
    string Status,
    DateTime? ProcessedAt,
    string? ErrorMessage,
    bool CanReprocess);

// ── Generic ──────────────────────────────────────────────────────────────────

public sealed record CountDto
{
    public int Count { get; init; }
}

public sealed record MessageDto
{
    public required string Message { get; init; }
}

public sealed record ErrorDto
{
    public required string Error { get; init; }
}

// ── Audit Logs ───────────────────────────────────────────────────────────────

public sealed record AuditLogDto
{
    public required string Id { get; init; }
    public required string EventType { get; init; }
    public required string Action { get; init; }
    public required string Severity { get; init; }
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public string? IpAddress { get; init; }
    public string? CorrelationId { get; init; }
    public string? EntityId { get; init; }
    public string? EntityType { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Details { get; init; }
    public DateTime CreatedAt { get; init; }
}

// ── Dashboard ────────────────────────────────────────────────────────────────

public sealed record DashboardSummaryDto
{
    public int TotalStudies { get; init; }
    public int TotalPatients { get; init; }
    public int TotalNodes { get; init; }
    public int ActiveNodes { get; init; }
    public int PendingPacsStudies { get; init; }
    public int FailedStudies { get; init; }
    public QueueSummaryDto QueueSummary { get; init; } = new();
    public Hl7ListenerStatusDto? Hl7Status { get; init; }
    public IReadOnlyList<StudyDto> RecentStudies { get; init; } = [];
    public IReadOnlyList<NodeDto> Nodes { get; init; } = [];
}

// ── CSV Import/Export ────────────────────────────────────────────────────────

public sealed record CsvExportResultDto
{
    public required byte[] FileContent { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public int RecordCount { get; init; }
}

public sealed record ImportResultDto
{
    public int TotalRecords { get; init; }
    public int SuccessCount { get; init; }
    public int ErrorCount { get; init; }
    public IReadOnlyList<ImportRowResult> Rows { get; init; } = [];
}

public sealed record ImportRowResult
{
    public int RowNumber { get; init; }
    public required string Status { get; init; }
    public string? Identifier { get; init; }
    public string? Error { get; init; }
}
