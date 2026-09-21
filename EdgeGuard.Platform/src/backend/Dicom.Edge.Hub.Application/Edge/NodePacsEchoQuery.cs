using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;

namespace Dicom.Edge.Hub.Application.Edge;

/// <summary>
/// Estado de conectividad C-ECHO de un nodo, tomando lo más fresco que haya.
/// </summary>
public interface INodePacsEchoQuery
{
    Task<NodePacsCEchoStatusDto?> GetAsync(string nodeId, CancellationToken ct = default);
}

/// <summary>
/// Combina las dos fuentes de conectividad, que existen por razones distintas.
///
/// <para><b>El store en memoria</b> es la vista viva: la escribe cada reporte del nodo y
/// conserva todo lo que el nodo midió. Es la buena, mientras exista.</para>
///
/// <para><b>Las asignaciones persistidas</b> son la red de seguridad. El store se vacía en
/// cada reciclado del pool de IIS, y sin este respaldo la pantalla pasaba a decir "el nodo
/// aún no ha reportado estado C-ECHO" aunque el nodo llevara meses reportando — y seguía
/// diciéndolo hasta que venciera el intervalo C-ECHO de cada nodo, que es configurable y
/// puede ser de horas. Con el respaldo, un reinicio deja un hueco de un ciclo en vez de un
/// vacío total.</para>
///
/// <para>Todo esto es <b>por nodo</b>. Un resultado C-ECHO es del par (nodo, PACS): lo
/// produce un nodo concreto sondeando desde su red contra un AE llamado concreto. Dos
/// nodos pueden reportar veredictos opuestos sobre el mismo PACS y tener razón los dos,
/// así que no existe un estado "del PACS" que agregar.</para>
/// </summary>
public sealed class NodePacsEchoQuery(
    INodePacsEchoStore store,
    INodeRepository nodeRepository,
    IPacsServerRepository pacsRepository) : INodePacsEchoQuery
{
    public async Task<NodePacsCEchoStatusDto?> GetAsync(string nodeId, CancellationToken ct = default)
    {
        // La vista viva gana siempre que exista: trae lo mismo que la persistida y además
        // es la que el nodo acaba de escribir.
        var vivo = store.Get(nodeId);
        if (vivo is not null) return vivo;

        var node = await nodeRepository.GetWithPacsAssignmentsAsync(nodeId, ct);
        if (node is null) return null;

        // Sólo las asignaciones con un sondeo registrado. Una recién creada todavía no
        // tiene veredicto, y publicarla como fallo sería inventarse un resultado.
        var sondeadas = node.PacsAssignments
            .Where(a => a.LastCEchoAt is not null && a.LastCEchoSuccess is not null)
            .ToList();

        if (sondeadas.Count == 0) return null;

        var pacsPorId = (await pacsRepository.GetAllAsync(ct)).ToDictionary(p => p.Id);

        var destinos = sondeadas
            .Select(a => (asignacion: a, pacs: pacsPorId.GetValueOrDefault(a.PacsId)))
            .Where(x => x.pacs is not null)
            .Select(x => new PacsCEchoDestinationDto
            {
                AeTitle      = x.pacs!.AeTitle.Value,
                Host         = x.pacs.HostName,
                Port         = x.pacs.Port,
                Success      = x.asignacion.LastCEchoSuccess!.Value,
                LatencyMs    = x.asignacion.LastCEchoLatencyMs,
                Error        = x.asignacion.LastCEchoError,
                ErrorReason  = x.asignacion.LastCEchoErrorReason,
                CheckedAtUtc = x.asignacion.LastCEchoAt!.Value,
            })
            .ToList();

        if (destinos.Count == 0) return null;

        return new NodePacsCEchoStatusDto
        {
            NodeId = nodeId,
            // El sondeo más reciente de los conservados. El reporte original era un lote
            // con una sola hora; reconstruido, cada destino trae la suya y la del conjunto
            // es la última, que es lo que la pantalla muestra como "reportado hace…".
            ReportedAtUtc = destinos.Max(d => d.CheckedAtUtc),
            Destinations  = destinos.AsReadOnly(),
        };
    }
}
