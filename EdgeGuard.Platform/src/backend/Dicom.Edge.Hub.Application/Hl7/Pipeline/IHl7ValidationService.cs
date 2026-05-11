using Dicom.Edge.Hub.Domain.Entities;

namespace Dicom.Edge.Hub.Application.Hl7.Pipeline;

/// <summary>
/// Enterprise validation for HL7 ADT, ORM, and ORU messages.
/// </summary>
public interface IHl7ValidationService
{
    Hl7ValidationResult Validate(Hl7Message message);
}
