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

    public DateTime? BirthDate { get; init; }

    [StringLength(10)]
    public string? Sex { get; init; }

    [StringLength(20)]
    public string? PhoneNumber { get; init; }

    [StringLength(256)]
    public string? Email { get; init; }
}

// ── Update Study Status (manual) ─────────────────────────────────────────────

public sealed class UpdateStudyStatusRequest
{
    [Required]
    public required string Status { get; init; }

    [StringLength(500)]
    public string? Reason { get; init; }
}

// ── Node Configuration ───────────────────────────────────────────────────────

public sealed class UpdateNodeSettingRequest
{
    [Required]
    public required string Value { get; init; }
}
