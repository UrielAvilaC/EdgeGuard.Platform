namespace Dicom.Edge.Node.Persistence.Services;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IEquipmentActivityTracker"/>.
/// Backed by a <see cref="ConcurrentDictionary{TKey,TValue}"/> so the DICOM hot path
/// (association accept) never blocks.
/// </summary>
public sealed class InMemoryEquipmentActivityTracker : IEquipmentActivityTracker
{
    private readonly ConcurrentDictionary<string, DateTime> _lastSeen =
        new(StringComparer.OrdinalIgnoreCase);

    public void RecordSeen(string aeTitle)
    {
        if (string.IsNullOrWhiteSpace(aeTitle)) return;
        _lastSeen[aeTitle.Trim()] = DateTime.UtcNow;
    }

    public IReadOnlyDictionary<string, DateTime> GetSnapshot() =>
        new Dictionary<string, DateTime>(_lastSeen, StringComparer.OrdinalIgnoreCase);
}
