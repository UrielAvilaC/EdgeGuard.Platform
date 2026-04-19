namespace Dicom.Edge.Common.Filters;

/// <summary>
/// Base record with sorting and pagination metadata.
/// All domain-level filter records should inherit from this.
/// </summary>
public abstract record FilterBase
{
    public string? SortBy { get; init; }
    public string? SortDir { get; init; }
}

/// <summary>
/// Domain filter for Study queries. Used by IStudyRepository.
/// </summary>
public sealed record StudyFilterCriteria : FilterBase
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? SourceNodeId { get; init; }
    public string? PatientId { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public bool? IsUrgent { get; init; }
}

/// <summary>
/// Domain filter for Patient queries. Used by IPatientRepository.
/// </summary>
public sealed record PatientFilterCriteria : FilterBase
{
    public string? Search { get; init; }
    public string? CreatedByNodeId { get; init; }
    public bool? IsActive { get; init; }
    public bool? HasPhone { get; init; }
    public bool? HasEmail { get; init; }
}

/// <summary>
/// Domain filter for AuditLog queries. Used by IHubAuditLogRepository.
/// </summary>
public sealed record AuditLogFilterCriteria : FilterBase
{
    public string? EventType { get; init; }
    public string? Severity { get; init; }
    public string? UserId { get; init; }
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public bool? IsSuccess { get; init; }
    public string? Search { get; init; }
}

/// <summary>
/// Domain filter for Node queries. Used by INodeRepository.
/// </summary>
public sealed record NodeFilterCriteria : FilterBase
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public bool? IsEnabled { get; init; }
}

/// <summary>
/// Domain filter for PacsServer queries. Used by IPacsServerRepository.
/// </summary>
public sealed record PacsServerFilterCriteria : FilterBase
{
    public string? Search { get; init; }
    public bool? IsEnabled { get; init; }
    public bool? IsGlobal { get; init; }
}

/// <summary>
/// Domain filter for HL7 Routing Rule queries. Used by IHl7RoutingRuleRepository.
/// </summary>
public sealed record RoutingRuleFilterCriteria : FilterBase
{
    public string? Search { get; init; }
    public bool? IsEnabled { get; init; }
    public string? TargetNodeId { get; init; }
}

/// <summary>
/// Domain filter for User queries. Used by IUserRepository.
/// </summary>
public sealed record UserFilterCriteria : FilterBase
{
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public string? Role { get; init; }
}

/// <summary>
/// Domain filter for HL7 Message queries. Used by IHl7MessageRepository.
/// </summary>
public sealed record Hl7MessageFilterCriteria : FilterBase
{
    public string? MessageType { get; init; }
    public string? DispatchStatus { get; init; }
    public string? TargetNodeId { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
}
