namespace Dicom.Edge.Contracts.Hub;

// ── Patients ─────────────────────────────────────────────────────────────────

public sealed record PatientDto
{
    public required string Id { get; init; }
    public required string PatientDicomId { get; init; }
    public required string PatientName { get; init; }
    public DateTime? BirthDate { get; init; }
    public string? Sex { get; init; }
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
    public long MaxStorageMb { get; init; }
    public long AvailableStorageMb { get; init; }
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

// ── Studies ──────────────────────────────────────────────────────────────────

public sealed record StudyDto
{
    public required string Id { get; init; }
    public required string StudyInstanceUid { get; init; }
    public string? AccessionNumber { get; init; }
    public DateTime? StudyDate { get; init; }
    public string? StudyDescription { get; init; }
    public string? ReferringPhysician { get; init; }
    public string? PatientId { get; init; }
    public string? PatientName { get; init; }
    public string? SourceNodeId { get; init; }
    public string? SourceAeTitle { get; init; }
    public required string Status { get; init; }
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
    string? ErrorMessage);

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
    string? ErrorMessage);

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
