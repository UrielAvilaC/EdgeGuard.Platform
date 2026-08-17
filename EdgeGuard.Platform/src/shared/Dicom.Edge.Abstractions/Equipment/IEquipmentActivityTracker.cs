namespace Dicom.Edge.Abstractions.Equipment;

/// <summary>
/// Thread-safe in-memory tracker of the most recent association time per equipment
/// (by calling AE title). Written on the DICOM hot path by the SCP and read by the
/// presence reporter, which forwards deltas to the Hub. Presence is fully passive —
/// derived from associations the equipment actually opens, never from active polling.
/// </summary>
public interface IEquipmentActivityTracker
{
    /// <summary>Records that the given calling AE was just seen (sets last-seen = now).</summary>
    void RecordSeen(string aeTitle);

    /// <summary>Returns a snapshot copy of the current last-seen map (AE → UTC time).</summary>
    IReadOnlyDictionary<string, DateTime> GetSnapshot();
}
