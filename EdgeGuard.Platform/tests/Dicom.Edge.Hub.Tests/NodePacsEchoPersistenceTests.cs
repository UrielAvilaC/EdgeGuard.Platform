using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Application.Edge;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Hub.Persistence.Context;
using Dicom.Edge.Hub.Persistence.Repositories;
using Dicom.Edge.Hub.Persistence.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dicom.Edge.Hub.Tests;

/// <summary>
/// Fija cómo se guarda y se recupera la conectividad C-ECHO.
///
/// <para><b>Es del par (nodo, PACS), nunca del PACS.</b> El sondeo lo hace cada nodo desde
/// su propia red contra un AE llamado concreto, así que dos nodos pueden emitir veredictos
/// opuestos sobre el mismo servidor y tener razón los dos: distinta ruta, distinto
/// firewall, distinta lista de AEs aceptados. Un campo "alcanzable" en <c>PacsServer</c>
/// no puede representar eso —tendría que elegir un ganador— y por eso se eliminó.</para>
///
/// <para>El otro punto que se fija aquí es la supervivencia al reinicio. La vista viva es
/// un store en memoria que el reciclado del pool de IIS vacía; sin respaldo persistido la
/// pantalla pasaba a decir "el nodo aún no ha reportado" aunque el nodo llevara meses
/// haciéndolo, y lo seguía diciendo hasta que venciera el intervalo C-ECHO de ese nodo.</para>
/// </summary>
public class NodePacsEchoPersistenceTests : IClassFixture<HubSqliteFixture>
{
    private readonly HubSqliteFixture _db;

    public NodePacsEchoPersistenceTests(HubSqliteFixture db) => _db = db;

    /// <summary>
    /// La prueba central del modelo: el mismo PACS, dos nodos, veredictos opuestos. Cada
    /// uno conserva el suyo. Con la conectividad guardada en <c>PacsServer</c> esto era
    /// imposible de representar: el segundo reporte pisaba al primero.
    /// </summary>
    [Fact]
    public async Task DosNodos_ConVeredictosOpuestosSobreElMismoPacs_ConservanCadaUnoElSuyo()
    {
        var pacs = await SembrarPacs("PACS_COMPARTIDO");
        var nodoA = await SembrarNodoConPacs(pacs.Id);
        var nodoB = await SembrarNodoConPacs(pacs.Id);

        await Reportar(nodoA, pacs.AeTitle.Value, exito: true, latencyMs: 12);
        await Reportar(nodoB, pacs.AeTitle.Value, exito: false, errorReason: "CalledAENotRecognized");

        var estadoA = await Consultar(nodoA);
        var estadoB = await Consultar(nodoB);

        Assert.True(estadoA!.Destinations.Single().Success);
        Assert.Equal(12, estadoA.Destinations.Single().LatencyMs);

        Assert.False(estadoB!.Destinations.Single().Success);
        Assert.Equal("CalledAENotRecognized", estadoB.Destinations.Single().ErrorReason);
    }

    /// <summary>
    /// El reporte tiene que quedar escrito en la asignación, no sólo en memoria. Se
    /// verifica releyendo desde un contexto nuevo, no desde el que ejecutó la operación.
    /// </summary>
    [Fact]
    public async Task ReporteRecibido_QuedaPersistidoEnLaAsignacion()
    {
        var pacs = await SembrarPacs("PACS_PERSIST");
        var nodeId = await SembrarNodoConPacs(pacs.Id);

        await Reportar(nodeId, pacs.AeTitle.Value, exito: true, latencyMs: 34.5);

        await using var verificacion = _db.NewContext();
        var asignacion = await verificacion.NodePacsAssignments
            .AsNoTracking()
            .SingleAsync(a => a.NodeId == nodeId && a.PacsId == pacs.Id);

        Assert.True(asignacion.LastCEchoSuccess);
        Assert.Equal(34.5, asignacion.LastCEchoLatencyMs);
        Assert.NotNull(asignacion.LastCEchoAt);
    }

    /// <summary>
    /// El problema de fondo: tras reiniciar el Hub el store en memoria está vacío, y antes
    /// eso dejaba la tarjeta en "aún no ha reportado". Ahora se reconstruye desde la base.
    /// </summary>
    [Fact]
    public async Task TrasReiniciarElHub_ElEstadoSeReconstruyeDesdeLaBase()
    {
        var pacs = await SembrarPacs("PACS_REINICIO");
        var nodeId = await SembrarNodoConPacs(pacs.Id);

        await Reportar(nodeId, pacs.AeTitle.Value, exito: false, error: "Connection refused");

        // Store nuevo y vacío = exactamente lo que deja un reciclado del pool.
        var estado = await Consultar(nodeId);

        Assert.NotNull(estado);
        var destino = Assert.Single(estado!.Destinations);
        Assert.False(destino.Success);
        Assert.Equal("Connection refused", destino.Error);
        Assert.Equal(pacs.AeTitle.Value, destino.AeTitle);
    }

    /// <summary>
    /// Un éxito posterior tiene que limpiar el error anterior. Si no, la pantalla mostraría
    /// un motivo de fallo junto a un tick verde.
    /// </summary>
    [Fact]
    public async Task UnExitoPosterior_BorraElErrorAnterior()
    {
        var pacs = await SembrarPacs("PACS_RECUPERA");
        var nodeId = await SembrarNodoConPacs(pacs.Id);

        await Reportar(nodeId, pacs.AeTitle.Value, exito: false, errorReason: "CalledAENotRecognized");
        await Reportar(nodeId, pacs.AeTitle.Value, exito: true, latencyMs: 8);

        var destino = (await Consultar(nodeId))!.Destinations.Single();

        Assert.True(destino.Success);
        Assert.Null(destino.Error);
        Assert.Null(destino.ErrorReason);
        Assert.Equal(8, destino.LatencyMs);
    }

    /// <summary>
    /// Un destino que el nodo reporta pero que no tiene asignado no se guarda. Persistirlo
    /// exigiría inventar la asignación que le da sentido.
    /// </summary>
    [Fact]
    public async Task DestinoNoAsignado_NoSePersiste()
    {
        var pacs = await SembrarPacs("PACS_ASIGNADO");
        var ajeno = await SembrarPacs("PACS_AJENO");
        var nodeId = await SembrarNodoConPacs(pacs.Id);

        await Reportar(nodeId, ajeno.AeTitle.Value, exito: true);

        await using var verificacion = _db.NewContext();
        var asignaciones = await verificacion.NodePacsAssignments
            .AsNoTracking().Where(a => a.NodeId == nodeId).ToListAsync();

        Assert.All(asignaciones, a => Assert.Null(a.LastCEchoAt));
    }

    /// <summary>
    /// Una asignación sin sondear queda fuera del estado reconstruido: no hay veredicto
    /// todavía, y publicarla como fallo sería inventarse un resultado. "Desconocido" y
    /// "no alcanzable" no son lo mismo — confundirlos es lo que producía el badge rojo
    /// permanente que se quitó del diálogo de asignación.
    /// </summary>
    [Fact]
    public async Task AsignacionSinSondear_NoApareceComoFallo()
    {
        var pacs = await SembrarPacs("PACS_SIN_SONDEO");
        var nodeId = await SembrarNodoConPacs(pacs.Id);

        var estado = await Consultar(nodeId);

        Assert.Null(estado);
    }

    // ── Cableado ─────────────────────────────────────────────────────────────

    private async Task Reportar(
        string nodeId, string aeTitle, bool exito,
        double? latencyMs = null, string? error = null, string? errorReason = null)
    {
        await using var contexto = _db.NewContext();
        var writer = new NodePacsEchoWriter(
            new NodeRepository(contexto),
            new PacsServerRepository(contexto),
            new HubUnitOfWork(contexto, NullLogger<HubUnitOfWork>.Instance),
            NullLogger<NodePacsEchoWriter>.Instance);

        await writer.PersistAsync(new NodePacsEchoReportRequest
        {
            NodeId = nodeId,
            ReportedAtUtc = DateTime.UtcNow,
            Results =
            [
                new PacsEchoDestinationResult
                {
                    AeTitle = aeTitle,
                    Host = "127.0.0.1",
                    Port = 104,
                    Success = exito,
                    LatencyMs = latencyMs,
                    Error = error,
                    ErrorReason = errorReason,
                    CheckedAtUtc = DateTime.UtcNow,
                },
            ],
        });
    }

    private async Task<NodePacsCEchoStatusDto?> Consultar(string nodeId)
    {
        await using var contexto = _db.NewContext();
        var store = new NodePacsEchoStoreVacio();

        var query = new NodePacsEchoQuery(
            store, new NodeRepository(contexto), new PacsServerRepository(contexto));

        return await query.GetAsync(nodeId);
    }

    /// <summary>Un store siempre vacío: simula el Hub recién reiniciado.</summary>
    private sealed class NodePacsEchoStoreVacio : INodePacsEchoStore
    {
        public void Upsert(NodePacsCEchoStatusDto status) { }
        public NodePacsCEchoStatusDto? Get(string nodeId) => null;
    }

    // ── Sembrado ─────────────────────────────────────────────────────────────

    private async Task<PacsServer> SembrarPacs(string nombre)
    {
        await using var contexto = _db.NewContext();

        var pacs = PacsServer.Create(
            name: nombre,
            aeTitle: AeTitle.Create($"AE{Random.Shared.Next(100000, 999999)}"),
            hostName: "127.0.0.1",
            port: 104);

        contexto.PacsServers.Add(pacs);
        await contexto.SaveChangesAsync();
        return pacs;
    }

    private async Task<string> SembrarNodoConPacs(string pacsId)
    {
        await using var contexto = _db.NewContext();

        var nodo = Node.Create(
            name: $"NODO-{Guid.NewGuid():N}"[..20],
            aeTitle: AeTitle.Create($"ND{Random.Shared.Next(100000, 999999)}"),
            ipAddress: "127.0.0.1",
            port: 104);

        nodo.AssignPacs(pacsId);

        contexto.Nodes.Add(nodo);
        await contexto.SaveChangesAsync();
        return nodo.Id;
    }
}
