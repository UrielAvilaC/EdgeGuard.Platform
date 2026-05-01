using Dicom.Edge.Hub.Domain.Entities;

namespace Dicom.Edge.Hub.Application.Hl7;

/// <summary>
/// Syncs a study from an HL7 message.
/// Creates a new study with status <c>Scheduled</c> when an AccessionNumber is present.
/// Skips silently if the study already exists or data is insufficient.
/// </summary>
public interface IHl7StudySyncService
{
    Task SyncFromHl7Async(Hl7Message message, CancellationToken ct = default);
}
