using System.ComponentModel.DataAnnotations;

namespace Dicom.Edge.Contracts.Hub;

// ── Common filter base ───────────────────────────────────────────────────────

/// <summary>
/// Base filter for all paginated list endpoints.
/// </summary>
public abstract class PagedFilterBase
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 500)]
    public int PageSize { get; init; } = 25;

    /// <summary>Sort field name (entity-specific).</summary>
    public string? SortBy { get; init; }

    /// <summary>Sort direction: "asc" or "desc".</summary>
    public string? SortDir { get; init; }
}

// ── Studies ──────────────────────────────────────────────────────────────────

public sealed class StudyFilter : PagedFilterBase
{
    /// <summary>Free-text search across AccessionNumber, PatientName, StudyDescription.</summary>
    public string? Search { get; init; }

    public string? Status { get; init; }
    public string? SourceNodeId { get; init; }
    public string? PatientId { get; init; }
    public string? Modality { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public bool? IsUrgent { get; init; }
}

// ── Patients ─────────────────────────────────────────────────────────────────

public sealed class PatientFilter : PagedFilterBase
{
    /// <summary>Free-text search across PatientName, PatientDicomId.</summary>
    public string? Search { get; init; }

    public string? CreatedByNodeId { get; init; }
    public bool? IsActive { get; init; }
    public bool? HasPhone { get; init; }
    public bool? HasEmail { get; init; }
}

// ── Nodes ────────────────────────────────────────────────────────────────────

public sealed class NodeFilter : PagedFilterBase
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public bool? IsEnabled { get; init; }
}

// ── PACS Servers ─────────────────────────────────────────────────────────────

public sealed class PacsServerFilter : PagedFilterBase
{
    public string? Search { get; init; }
    public bool? IsEnabled { get; init; }
    public bool? IsGlobal { get; init; }
}

// ── Routing Rules ────────────────────────────────────────────────────────────

public sealed class RoutingRuleFilter : PagedFilterBase
{
    public string? Search { get; init; }
    public bool? IsEnabled { get; init; }
    public string? TargetNodeId { get; init; }
}

// ── Audit Logs ───────────────────────────────────────────────────────────────

public sealed class AuditLogFilter : PagedFilterBase
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

// ── HL7 Messages ─────────────────────────────────────────────────────────────

public sealed class Hl7MessageFilter : PagedFilterBase
{
    public string? MessageType { get; init; }
    public string? DispatchStatus { get; init; }
    public string? TargetNodeId { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
}

// ── WhatsApp Notifications ───────────────────────────────────────────────────

public sealed class WhatsAppNotificationFilter : PagedFilterBase
{
    public string? StudyId { get; init; }
    public string? Status { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
}

// ── Users ────────────────────────────────────────────────────────────────────

public sealed class UserFilter : PagedFilterBase
{
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public string? Role { get; init; }
}
