using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Equipment;

/// <summary>
/// Hub-managed equipment (a modality device) scoped to a single Edge Node.
/// Identified by its calling <see cref="AeTitle"/>, it carries the set of allowed
/// modality codes (<see cref="Modalities"/>) the node uses to filter the Modality
/// Worklist per device, plus optional station/IP reinforcement.
/// </summary>
public sealed class NodeEquipment : AggregateRoot<string>
{
    public string  NodeId         { get; private set; } = default!;
    public string  AeTitle        { get; private set; } = default!;
    public string? DisplayName    { get; private set; }
    public string? StationAeTitle { get; private set; }
    public string? StationName    { get; private set; }
    public string? IpAddress      { get; private set; }
    public bool    IsEnabled      { get; private set; }

    // ── Inventory metadata ──────────────────────────────────────────────────────
    public string? Location     { get; private set; }
    public string? Department   { get; private set; }
    public string? Manufacturer { get; private set; }
    public string? Model        { get; private set; }
    public string? Notes        { get; private set; }

    // ── Observability ────────────────────────────────────────────────────────────
    public DateTime? LastConnectionAt { get; private set; }
    public bool      IsOnline         { get; private set; }

    private readonly List<EquipmentModality> _modalities = [];
    public IReadOnlyCollection<EquipmentModality> Modalities => _modalities.AsReadOnly();

    /// <summary>The assigned modality codes (convenience projection over the join rows).</summary>
    public IReadOnlyList<string> ModalityCodes => _modalities.Select(m => m.ModalityCode).ToList();

    private NodeEquipment() { }

    public static NodeEquipment Create(
        string  nodeId,
        string  aeTitle,
        string? displayName    = null,
        string? stationAeTitle = null,
        string? stationName    = null,
        string? ipAddress      = null,
        string? location       = null,
        string? department     = null,
        string? manufacturer   = null,
        string? model          = null,
        string? notes          = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aeTitle);

        return new NodeEquipment
        {
            Id             = IdGenerator.NewId(),
            NodeId         = nodeId,
            AeTitle        = aeTitle.Trim().ToUpperInvariant(),
            DisplayName    = displayName?.Trim(),
            StationAeTitle = stationAeTitle?.Trim(),
            StationName    = stationName?.Trim(),
            IpAddress      = ipAddress?.Trim(),
            IsEnabled      = true,
            Location       = location?.Trim(),
            Department     = department?.Trim(),
            Manufacturer   = manufacturer?.Trim(),
            Model          = model?.Trim(),
            Notes          = notes?.Trim(),
        };
    }

    public void Update(
        string  aeTitle,
        string? displayName,
        string? stationAeTitle,
        string? stationName,
        string? ipAddress,
        string? location,
        string? department,
        string? manufacturer,
        string? model,
        string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aeTitle);

        AeTitle        = aeTitle.Trim().ToUpperInvariant();
        DisplayName    = displayName?.Trim();
        StationAeTitle = stationAeTitle?.Trim();
        StationName    = stationName?.Trim();
        IpAddress      = ipAddress?.Trim();
        Location       = location?.Trim();
        Department     = department?.Trim();
        Manufacturer   = manufacturer?.Trim();
        Model          = model?.Trim();
        Notes          = notes?.Trim();
        UpdatedAt      = DateTime.UtcNow;
    }

    /// <summary>
    /// Replaces the assigned modality set. Codes are normalized (trimmed, upper-cased,
    /// de-duplicated). Validation that each code is a supported &amp; active catalog entry
    /// is performed by the application service before calling this method.
    /// </summary>
    public void SetModalities(IEnumerable<string> codes)
    {
        var normalized = codes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _modalities.Clear();
        foreach (var code in normalized)
            _modalities.Add(new EquipmentModality(Id, code));

        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable()  { IsEnabled = true;  UpdatedAt = DateTime.UtcNow; }
    public void Disable() { IsEnabled = false; UpdatedAt = DateTime.UtcNow; }

    public void MarkConnected(DateTime atUtc)
    {
        LastConnectionAt = atUtc;
        IsOnline         = true;
        UpdatedAt        = DateTime.UtcNow;
    }
}
