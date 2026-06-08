using System.ComponentModel.DataAnnotations;

namespace Dicom.Edge.Contracts.Hub;

// ── Hub-managed equipment catalog (per node) ─────────────────────────────────

/// <summary>
/// A modality device (equipment) connected to an Edge Node. Drives both association
/// acceptance (by <see cref="AeTitle"/>) and per-equipment MWL filtering (by
/// <see cref="ModalityCodes"/> and optional <see cref="StationAeTitle"/>).
/// </summary>
public sealed class NodeEquipmentDto
{
    public string  Id             { get; init; } = default!;
    public string  NodeId         { get; init; } = default!;
    public string  AeTitle        { get; init; } = default!;
    public string? DisplayName    { get; init; }

    /// <summary>The set of allowed modality codes (from the catalog) for this equipment.</summary>
    public IReadOnlyList<string> ModalityCodes { get; init; } = [];

    public string? StationAeTitle { get; init; }
    public string? StationName    { get; init; }
    public string? IpAddress      { get; init; }
    public bool    IsEnabled      { get; init; }

    public string? Location       { get; init; }
    public string? Department     { get; init; }
    public string? Manufacturer   { get; init; }
    public string? Model          { get; init; }
    public string? Notes          { get; init; }

    public DateTime? LastConnectionAt { get; init; }
    public bool      IsOnline         { get; init; }
    public DateTime  CreatedAt        { get; init; }
    public DateTime? UpdatedAt        { get; init; }
}

public sealed class CreateNodeEquipmentRequest
{
    [Required, StringLength(16, MinimumLength = 1)]
    [RegularExpression(@"^[A-Z0-9_]+$", ErrorMessage = "AE Title must contain only uppercase letters, numbers, and underscores")]
    public required string AeTitle { get; init; }

    [StringLength(100)] public string? DisplayName { get; init; }

    /// <summary>Catalog codes to assign. Each must be a supported &amp; active modality.</summary>
    public IReadOnlyList<string> ModalityCodes { get; init; } = [];

    [StringLength(16)]  public string? StationAeTitle { get; init; }
    [StringLength(100)] public string? StationName    { get; init; }
    [StringLength(45)]  public string? IpAddress      { get; init; }

    [StringLength(200)] public string? Location     { get; init; }
    [StringLength(100)] public string? Department   { get; init; }
    [StringLength(100)] public string? Manufacturer { get; init; }
    [StringLength(100)] public string? Model        { get; init; }
    [StringLength(1000)] public string? Notes       { get; init; }
}

public sealed class UpdateNodeEquipmentRequest
{
    [Required, StringLength(16, MinimumLength = 1)]
    [RegularExpression(@"^[A-Z0-9_]+$", ErrorMessage = "AE Title must contain only uppercase letters, numbers, and underscores")]
    public required string AeTitle { get; init; }

    [StringLength(100)] public string? DisplayName { get; init; }

    public IReadOnlyList<string> ModalityCodes { get; init; } = [];

    [StringLength(16)]  public string? StationAeTitle { get; init; }
    [StringLength(100)] public string? StationName    { get; init; }
    [StringLength(45)]  public string? IpAddress      { get; init; }

    [StringLength(200)] public string? Location     { get; init; }
    [StringLength(100)] public string? Department   { get; init; }
    [StringLength(100)] public string? Manufacturer { get; init; }
    [StringLength(100)] public string? Model        { get; init; }
    [StringLength(1000)] public string? Notes       { get; init; }
}

// ── Node sync payload (Hub → Node) ──────────────────────────────────────────

public sealed class EquipmentSyncRequest
{
    public required string NodeId   { get; init; }
    public DateTime SyncedAtUtc     { get; init; } = DateTime.UtcNow;
    public required IReadOnlyList<EquipmentSyncEntry> Equipment { get; init; }
}

public sealed class EquipmentSyncEntry
{
    public required string Id      { get; init; }
    public required string AeTitle { get; init; }
    public string?  DisplayName    { get; init; }
    public required IReadOnlyList<string> ModalityCodes { get; init; }
    public string?  StationAeTitle { get; init; }
    public string?  StationName    { get; init; }
    public string?  IpAddress      { get; init; }
    public bool     IsEnabled      { get; init; }
}

public sealed class EquipmentSyncResponse
{
    public bool     Accepted     { get; init; }
    public int      AppliedCount { get; init; }
    public int      RemovedCount { get; init; }
    public DateTime AppliedAt    { get; init; }
    public string?  Error        { get; init; }
}
