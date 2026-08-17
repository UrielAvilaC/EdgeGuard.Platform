namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Options for the periodic equipment presence reporter (last-seen → Hub).
/// Bound from the <c>EquipmentPresence</c> configuration section.
/// </summary>
public sealed class EquipmentPresenceOptions
{
    public const string SectionName = "EquipmentPresence";

    /// <summary>Master switch for presence reporting.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How often to report equipment activity deltas to the Hub.</summary>
    public int IntervalSeconds { get; set; } = 60;
}
