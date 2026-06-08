namespace Dicom.Edge.Abstractions.Equipment;

/// <summary>
/// Immutable runtime snapshot of an equipment entry, used by the DICOM SCP to validate
/// associations and filter the Modality Worklist per device.
/// </summary>
public sealed record EquipmentCatalogEntry(
    string Id,
    string AeTitle,
    IReadOnlyList<string> ModalityCodes,
    string? StationAeTitle,
    string? IpAddress,
    bool IsEnabled);

/// <summary>
/// Thread-safe in-memory cache of the node's equipment catalog. Populated by the
/// equipment loader (from the SQLite store) on startup and refreshed on each Hub push.
/// Read by the DICOM SCP on the hot path (association + MWL), so reads must be lock-light.
/// </summary>
public interface IEquipmentCatalog
{
    /// <summary>True when no equipment has been loaded yet (enables legacy fallback).</summary>
    bool IsEmpty { get; }

    /// <summary>Atomically replaces the entire catalog snapshot.</summary>
    void Replace(IEnumerable<EquipmentCatalogEntry> entries);

    /// <summary>Finds an equipment by calling AE title (case-insensitive), or null.</summary>
    EquipmentCatalogEntry? FindByAeTitle(string aeTitle);
}
