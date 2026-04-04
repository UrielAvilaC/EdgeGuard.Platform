using Dicom.Edge.Node.Sender;

namespace Dicom.Edge.Node.Router;

/// <summary>
/// Defines a single routing rule that matches studies to PACS destinations.
/// </summary>
public sealed class RoutingRule
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Priority { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>Modality filter (e.g., "CT", "MR"). Null = matches all.</summary>
    public string? ModalityFilter { get; init; }

    /// <summary>Source AE title filter. Null = matches all.</summary>
    public string? SourceAeTitleFilter { get; init; }

    /// <summary>Institution name filter. Null = matches all.</summary>
    public string? InstitutionFilter { get; init; }

    /// <summary>If true, only match urgent studies.</summary>
    public bool UrgentOnly { get; init; }

    /// <summary>Destination PACS for matching studies.</summary>
    public required PacsDestination Destination { get; init; }

    public bool Matches(StudyRoutingContext context)
    {
        if (!IsEnabled) return false;
        if (ModalityFilter is not null && !string.Equals(ModalityFilter, context.Modality, StringComparison.OrdinalIgnoreCase)) return false;
        if (SourceAeTitleFilter is not null && !string.Equals(SourceAeTitleFilter, context.SourceAeTitle, StringComparison.OrdinalIgnoreCase)) return false;
        if (InstitutionFilter is not null && !string.Equals(InstitutionFilter, context.InstitutionName, StringComparison.OrdinalIgnoreCase)) return false;
        if (UrgentOnly && !context.IsUrgent) return false;
        return true;
    }
}
