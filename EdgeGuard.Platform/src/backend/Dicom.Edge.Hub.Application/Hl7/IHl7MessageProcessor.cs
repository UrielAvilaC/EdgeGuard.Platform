using Dicom.Edge.Hub.Domain.Entities;

namespace Dicom.Edge.Hub.Application.Hl7;

/// <summary>
/// Procesador de mensajes HL7.
/// </summary>
public interface IHl7MessageProcessor
{
    Task ProcessAsync(Hl7Message message, CancellationToken cancellationToken = default);
}
