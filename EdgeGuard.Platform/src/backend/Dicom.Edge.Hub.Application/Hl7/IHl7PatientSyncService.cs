using Dicom.Edge.Hub.Domain.Entities;

namespace Dicom.Edge.Hub.Application.Hl7;

/// <summary>
/// Creates or updates a Patient record from parsed HL7 PID fields.
/// </summary>
public interface IHl7PatientSyncService
{
    Task SyncFromHl7Async(Hl7Message message, CancellationToken ct = default);
}
