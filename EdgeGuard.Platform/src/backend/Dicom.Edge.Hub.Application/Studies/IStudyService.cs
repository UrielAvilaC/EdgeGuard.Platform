using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;

namespace Dicom.Edge.Hub.Application.Studies;

/// <summary>
/// Application service for Study aggregate write operations.
/// Encapsulates metadata updates, state transitions, and persistence.
/// Read operations remain at the controller-repository level (services handle writes only).
/// </summary>
public interface IStudyService
{
    /// <summary>Updates study metadata. Returns null if the study was not found.</summary>
    Task<Study?> UpdateAsync(string id, UpdateStudyRequest request, CancellationToken ct = default);

    /// <summary>
    /// Manually transitions a study to a new status.
    /// Returns <c>(study, null)</c> on success, <c>(null, error)</c> on failure.
    /// </summary>
    Task<(Study? Study, string? Error)> UpdateStatusAsync(string id, UpdateStudyStatusRequest request, CancellationToken ct = default);
}
