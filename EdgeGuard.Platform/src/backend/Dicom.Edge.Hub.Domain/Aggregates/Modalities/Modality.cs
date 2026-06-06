namespace Dicom.Edge.Hub.Domain.Aggregates.Modalities;

/// <summary>
/// Reference catalog entry for a DICOM modality (Defined Term), keyed by <see cref="Code"/>.
/// Seeded automatically and kept in sync by the modality seeder. Only entries with
/// <see cref="IsSupported"/> = <c>true</c> (image-level modalities) may be linked to
/// equipment or used for MWL filtering. <see cref="IsActive"/> is operator-controlled and
/// preserved across re-seeds.
/// </summary>
public sealed class Modality
{
    public string Code        { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public bool   IsSupported { get; private set; }
    public bool   IsActive    { get; private set; }
    public int    SortOrder   { get; private set; }

    private Modality() { }

    public static Modality Create(
        string code, string displayName, bool isSupported, int sortOrder, bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new Modality
        {
            Code        = code.Trim().ToUpperInvariant(),
            DisplayName = displayName.Trim(),
            IsSupported = isSupported,
            IsActive    = isActive,
            SortOrder   = sortOrder,
        };
    }

    /// <summary>
    /// Refreshes seed-owned fields on re-seed. <see cref="IsActive"/> is intentionally
    /// NOT touched so operator toggles survive upgrades.
    /// </summary>
    public void UpdateFromSeed(string displayName, bool isSupported, int sortOrder)
    {
        DisplayName = displayName.Trim();
        IsSupported = isSupported;
        SortOrder   = sortOrder;
    }

    public void SetActive(bool active) => IsActive = active;

    /// <summary>A modality can be assigned/used only when it is both supported and active.</summary>
    public bool IsAssignable => IsSupported && IsActive;
}
