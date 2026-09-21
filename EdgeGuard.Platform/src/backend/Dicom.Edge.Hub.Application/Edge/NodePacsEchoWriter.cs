using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Edge;

/// <summary>
/// Baja un reporte C-ECHO a las asignaciones del nodo, para que sobreviva al reinicio.
/// </summary>
public interface INodePacsEchoWriter
{
    /// <summary>
    /// Persiste los resultados que correspondan a un PACS asignado al nodo.
    /// Devuelve cuántos se aplicaron. Nunca lanza.
    /// </summary>
    Task<int> PersistAsync(NodePacsEchoReportRequest request, CancellationToken ct = default);
}

/// <summary>
/// Escribe el resultado en <see cref="NodePacsAssignment"/> —el par (nodo, PACS)— y no en
/// <c>PacsServer</c>.
///
/// <para>La distinción no es de estilo. El sondeo lo hace un nodo concreto, desde su red,
/// contra un AE llamado concreto: dos nodos pueden emitir veredictos opuestos sobre el
/// mismo servidor y acertar los dos. Un campo en el catálogo de PACS tendría que elegir un
/// ganador, y el último reporte en llegar pisaría al resto.</para>
/// </summary>
public sealed class NodePacsEchoWriter(
    INodeRepository nodeRepository,
    IPacsServerRepository pacsRepository,
    IUnitOfWork unitOfWork,
    ILogger<NodePacsEchoWriter> logger) : INodePacsEchoWriter
{
    public async Task<int> PersistAsync(
        NodePacsEchoReportRequest request, CancellationToken ct = default)
    {
        try
        {
            // Lectura rastreada: las asignaciones se van a modificar.
            var node = await nodeRepository.GetWithPacsAssignmentsAsync(request.NodeId, ct);
            if (node is null)
            {
                logger.LogDebug(
                    "PACS echo report from unknown node {NodeId} — kept in the live store only",
                    request.NodeId);
                return 0;
            }

            // El reporte identifica destinos por AE title; las asignaciones, por pacsId.
            var pacsPorAe = (await pacsRepository.GetAllAsync(ct))
                .GroupBy(p => p.AeTitle.Value, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            var asignaciones = node.PacsAssignments.ToDictionary(a => a.PacsId);

            var aplicados = 0;
            foreach (var r in request.Results)
            {
                // Un destino que el nodo sondea pero no tiene asignado no se guarda:
                // persistirlo exigiría inventar la asignación que le da sujeto.
                if (!pacsPorAe.TryGetValue(r.AeTitle, out var pacsId)) continue;
                if (!asignaciones.TryGetValue(pacsId, out var asignacion)) continue;

                asignacion.UpdateCEchoResult(
                    r.Success, r.CheckedAtUtc, r.LatencyMs, r.Error, r.ErrorReason);
                aplicados++;
            }

            if (aplicados == 0) return 0;

            await unitOfWork.SaveChangesAsync(ct);

            logger.LogDebug(
                "PACS echo persisted for node {NodeId}: {Applied}/{Reported} destination(s) matched an assignment",
                request.NodeId, aplicados, request.Results.Count);

            return aplicados;
        }
        catch (Exception ex)
        {
            // Un fallo al persistir no puede convertir en error una llamada del nodo que
            // ya se atendió: el dato vivo quedó en el store y el próximo ciclo reintenta.
            logger.LogWarning(ex,
                "Could not persist the PACS echo report for node {NodeId} — the live store keeps it " +
                "and the node's next cycle retries",
                request.NodeId);
            return 0;
        }
    }
}
