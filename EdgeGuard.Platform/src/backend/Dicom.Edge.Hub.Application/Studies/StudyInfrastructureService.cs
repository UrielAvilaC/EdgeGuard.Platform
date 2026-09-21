using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;

namespace Dicom.Edge.Hub.Application.Studies;

/// <summary>
/// Builds the Infraestructura card for a study by resolving its origin node and target
/// PACS, including live-ish connectivity (node heartbeat status and the last C-ECHO result
/// for the node → PACS pair).
/// </summary>
public sealed class StudyInfrastructureService(
    IStudyRepository studyRepository,
    INodeRepository nodeRepository,
    IPacsServerRepository pacsRepository) : IStudyInfrastructureService
{
    public async Task<StudyInfrastructureDto?> GetAsync(string studyId, CancellationToken ct = default)
    {
        var study = await studyRepository.GetByIdAsync(studyId, ct);
        if (study is null) return null;

        // Origin node (with PACS assignments so we can read the node→PACS C-ECHO result).
        var node = !string.IsNullOrWhiteSpace(study.SourceNodeId)
            ? await nodeRepository.GetWithPacsAssignmentsAsync(study.SourceNodeId, ct)
            : null;

        // Target PACS identity.
        var pacs = !string.IsNullOrWhiteSpace(study.TargetPacsId)
            ? await pacsRepository.GetByIdAsync(study.TargetPacsId!, ct)
            : null;

        // Alcanzabilidad del par (nodo, PACS), que es el único nivel en que la pregunta
        // tiene respuesta: la sondea este nodo desde su red contra ese AE. Sin sondeo
        // registrado queda null —desconocido—, que no es lo mismo que "no alcanzable".
        //
        // Aquí había además un fallback al último C-ECHO del propio PacsServer. Era una
        // rama muerta: nadie escribía esos campos, así que la condición nunca se cumplía.
        // Y conceptualmente tampoco servía, porque un veredicto "del PACS" no existe.
        bool? pacsReachable = null;
        DateTime? pacsLastEchoAt = null;

        var assignment = node?.PacsAssignments
            .FirstOrDefault(a => a.PacsId == study.TargetPacsId);

        if (assignment?.LastCEchoAt is not null)
        {
            pacsReachable = assignment.LastCEchoSuccess;
            pacsLastEchoAt = assignment.LastCEchoAt;
        }

        return new StudyInfrastructureDto
        {
            SourceNodeId        = study.SourceNodeId,
            SourceNodeName      = node?.Name,
            SourceAeTitle       = study.SourceAeTitle ?? node?.AeTitle.Value,
            NodeStatus          = node?.Status.ToString(),
            NodeLastHeartbeatAt = node?.LastHeartbeatAt,

            TargetPacsId        = study.TargetPacsId,
            TargetPacsName      = pacs?.Name,
            TargetPacsAeTitle   = pacs?.AeTitle.Value,
            PacsReachable       = pacsReachable,
            PacsLastEchoAt      = pacsLastEchoAt,

            SentToPacsAt        = study.SentToPacsAt,
            PacsSendAttempts    = study.PacsSendAttempts,
            PacsSendLastError   = study.PacsSendLastError,
        };
    }
}
