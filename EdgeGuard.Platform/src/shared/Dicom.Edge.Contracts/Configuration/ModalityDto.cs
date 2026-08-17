namespace Dicom.Edge.Contracts.Configuration;

/// <summary>
/// Reference catalog entry for a DICOM modality (Defined Term).
/// Seeded automatically from <see cref="ModalitySeed"/>; only entries with
/// <see cref="IsSupported"/> = <c>true</c> (image-level modalities) may be linked to
/// equipment or used for Modality Worklist (MWL) filtering.
/// </summary>
public sealed class ModalityDto
{
    /// <summary>DICOM Defined Term, e.g. <c>MR</c>, <c>US</c>, <c>MG</c>, <c>DX</c>.</summary>
    public string Code { get; init; } = default!;

    /// <summary>Human-readable name (Spanish), e.g. "Resonancia Magnética".</summary>
    public string DisplayName { get; init; } = default!;

    /// <summary>True for image-producing modalities; false for non-image objects (SR, KO, …).</summary>
    public bool IsSupported { get; init; }

    /// <summary>Operator-controlled soft switch; an inactive code cannot be assigned.</summary>
    public bool IsActive { get; init; }

    /// <summary>Ordering hint for UI lists.</summary>
    public int SortOrder { get; init; }
}
