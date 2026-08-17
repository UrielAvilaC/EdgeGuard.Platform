namespace Dicom.Edge.Hub.Application.Equipment;

/// <summary>
/// Hub-side options for deriving equipment "online" status from the last-seen timestamp.
/// Bound from the <c>EquipmentPresence</c> configuration section (appsettings).
/// </summary>
public sealed class EquipmentPresenceOptions
{
    public const string SectionName = "EquipmentPresence";

    /// <summary>
    /// An equipment is considered "online" when its last association is within this many
    /// minutes. Should be comfortably larger than the node's presence report interval so a
    /// single missed report does not flap the indicator. Default: 10 minutes.
    /// </summary>
    public int OnlineWindowMinutes { get; set; } = 10;

    /// <summary>The online window as a <see cref="TimeSpan"/>.</summary>
    public TimeSpan OnlineWindow => TimeSpan.FromMinutes(Math.Max(1, OnlineWindowMinutes));
}
