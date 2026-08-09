using Dicom.Edge.Contracts.Hub;

namespace Dicom.Edge.Hub.Application.Studies;

public interface IStudyInfrastructureService
{
    /// <summary>
    /// Resolves the infrastructure view (origin node + target PACS identity and
    /// connectivity) for a study's detail page. Returns null when the study does not exist.
    /// </summary>
    Task<StudyInfrastructureDto?> GetAsync(string studyId, CancellationToken ct = default);
}
