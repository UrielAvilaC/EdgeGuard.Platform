namespace Dicom.Edge.Node.Persistence.Services;

/// <summary>
/// Thread-safe in-memory equipment catalog. The snapshot dictionary is swapped atomically
/// on <see cref="Replace"/> so readers on the DICOM hot path never lock.
/// </summary>
public sealed class InMemoryEquipmentCatalog : IEquipmentCatalog
{
    private volatile Dictionary<string, EquipmentCatalogEntry> _byAeTitle =
        new(StringComparer.OrdinalIgnoreCase);

    public bool IsEmpty => _byAeTitle.Count == 0;

    public void Replace(IEnumerable<EquipmentCatalogEntry> entries)
    {
        var snapshot = new Dictionary<string, EquipmentCatalogEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
            snapshot[entry.AeTitle.Trim()] = entry;

        _byAeTitle = snapshot;
    }

    public EquipmentCatalogEntry? FindByAeTitle(string aeTitle) =>
        _byAeTitle.TryGetValue(aeTitle.Trim(), out var entry) ? entry : null;
}
