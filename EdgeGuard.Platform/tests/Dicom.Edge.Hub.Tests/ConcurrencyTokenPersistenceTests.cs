using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Hub.Infrastructure.HostedServices;
using Dicom.Edge.Hub.Infrastructure.Services;
using Dicom.Edge.Hub.Persistence.Context;
using Dicom.Edge.Hub.Persistence.Repositories;
using Dicom.Edge.Hub.Persistence.UnitOfWork;
using Dicom.Edge.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dicom.Edge.Hub.Tests;

/// <summary>
/// Fija el contrato de escritura de las entidades con token de concurrencia optimista
/// (<c>Node</c> y <c>Study</c>, ambas con <c>UpdatedAt</c> como token).
///
/// <para>El defecto que motiva este archivo no es un caso borde: el evaluador de salud
/// leía los nodos con <c>AsNoTracking()</c>, los mutaba y los reataba con
/// <c>DbSet.Update()</c>. Al adjuntar una entidad desconectada, EF fija los valores
/// originales iguales a los actuales, y como todo mutador reescribe <c>UpdatedAt</c>, el
/// <c>WHERE</c> del <c>UPDATE</c> salía con el timestamp nuevo. Cero filas afectadas,
/// <see cref="DbUpdateConcurrencyException"/> hablando de una concurrencia que no existía,
/// y un nodo que estuvo veinte horas caído figurando <c>Online</c> en el panel.</para>
///
/// <para>Lo que hace falta subrayar es <b>por qué no lo detectaba nada</b>. La entidad en
/// memoria sí cambia: <c>MarkOffline()</c> hace su trabajo. Cualquier aserción sobre el
/// objeto —o sobre el mismo contexto que ejecutó la operación, que responde desde el
/// rastreador de cambios— da verde con el bug presente. La única aserción que lo ve es
/// releer la fila desde un contexto nuevo. Por eso todas las pruebas de aquí verifican
/// contra la base, y no contra el objeto.</para>
/// </summary>
public class ConcurrencyTokenPersistenceTests : IClassFixture<HubSqliteFixture>
{
    private readonly HubSqliteFixture _db;

    public ConcurrencyTokenPersistenceTests(HubSqliteFixture db) => _db = db;

    // ── Node: el camino que falló en producción ──────────────────────────────

    /// <summary>
    /// El caso exacto del log: un nodo habilitado cuyo último latido quedó muy fuera del
    /// período de gracia debe terminar <c>Offline</c> <b>en la base</b>.
    /// </summary>
    [Fact]
    public async Task NodoSinLatidos_QuedaOfflinePersistidoEnLaBase()
    {
        var nodeId = await SembrarNodoOnlineConLatidoAntiguo(TimeSpan.FromHours(7));

        await using (var contexto = _db.NewContext())
        {
            await CrearEvaluador(contexto).EvaluateAllNodesAsync();
        }

        await using var verificacion = _db.NewContext();
        var persistido = await verificacion.Nodes.AsNoTracking().SingleAsync(n => n.Id == nodeId);

        Assert.Equal(NodeStatus.Offline, persistido.Status);
    }

    /// <summary>
    /// El ciclo completo no debe lanzar. Antes lanzaba en cada iteración, y el hosted
    /// service lo registraba como <c>Error</c>: 751 en trece horas por una sola condición.
    /// </summary>
    [Fact]
    public async Task CicloDeEvaluacion_NoLanzaAlPersistirTransiciones()
    {
        await SembrarNodoOnlineConLatidoAntiguo(TimeSpan.FromHours(7));

        await using var contexto = _db.NewContext();
        var evaluador = CrearEvaluador(contexto);

        var excepcion = await Record.ExceptionAsync(() => evaluador.EvaluateAllNodesAsync());

        Assert.Null(excepcion);
    }

    /// <summary>
    /// Si alguien vuelve a poner una lectura sin rastreo en el camino de escritura, el
    /// ciclo tiene que <b>romperse ruidosamente</b>.
    ///
    /// <para>Esta prueba existe por una interacción que es fácil de romper sin notarlo. El
    /// ciclo ahora atrapa <see cref="DbUpdateConcurrencyException"/> y la degrada a
    /// Warning, porque un latido entrante durante la evaluación es una condición esperada.
    /// Pero el defecto original se manifestaba con <i>esa misma excepción</i>: si la
    /// lectura sin rastreo volviera, el <c>catch</c> se la tragaría y quedaríamos peor que
    /// antes —nodos que nunca pasan a Offline y ni siquiera un Error en el log—.</para>
    ///
    /// <para>Lo que evita ese escenario es que la guarda lance
    /// <see cref="InvalidOperationException"/>, que el <c>catch</c> no cubre. Es decir: el
    /// <c>catch</c> sólo es seguro de tener <em>porque</em> la guarda existe. Si alguien
    /// ensancha ese <c>catch</c> a <c>Exception</c>, esta prueba se pone roja.</para>
    /// </summary>
    [Fact]
    public async Task LecturaSinRastreoReintroducida_RompeElCicloEnVezDeSilenciarlo()
    {
        await SembrarNodoOnlineConLatidoAntiguo(TimeSpan.FromHours(7));

        await using var contexto = _db.NewContext();
        var evaluador = new NodeHealthEvaluator(
            new RepositorioQueOlvidaRastrear(contexto),
            new HealthCheckRepository(contexto),
            new HubUnitOfWork(contexto, NullLogger<HubUnitOfWork>.Instance),
            Options.Create(new NodeHealthThresholds()),
            NullLogger<NodeHealthEvaluator>.Instance);

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => evaluador.EvaluateAllNodesAsync());

        Assert.Contains("desconectada", excepcion.Message);
    }

    /// <summary>
    /// El repositorio real salvo por un detalle: devuelve los nodos sin rastrear. Es la
    /// regresión concreta que se quiere detectar, y por eso todo lo demás —incluida la
    /// guarda de <c>UpdateAsync</c>— tiene que ser el código de producción de verdad.
    /// </summary>
    private sealed class RepositorioQueOlvidaRastrear(HubDbContext contexto) : INodeRepository
    {
        private readonly NodeRepository _real = new(contexto);

        public async Task<IReadOnlyList<Node>> GetAllForUpdateAsync(CancellationToken ct = default) =>
            await contexto.Nodes.AsNoTracking().ToListAsync(ct);

        public Task<Node?> GetByIdAsync(string id, CancellationToken ct = default) => _real.GetByIdAsync(id, ct);
        public Task<Node?> GetByNameAndIpAsync(string name, string ip, CancellationToken ct = default) => _real.GetByNameAndIpAsync(name, ip, ct);
        public Task<IReadOnlyList<Node>> GetAllAsync(CancellationToken ct = default) => _real.GetAllAsync(ct);
        public Task<IReadOnlyList<Node>> GetActiveNodesAsync(CancellationToken ct = default) => _real.GetActiveNodesAsync(ct);
        public Task<Node?> GetWithPacsAssignmentsAsync(string id, CancellationToken ct = default) => _real.GetWithPacsAssignmentsAsync(id, ct);
        public Task<PagedResult<Node>> GetPagedAsync(PaginationRequest p, CancellationToken ct = default) => _real.GetPagedAsync(p, ct);
        public Task<PagedResult<Node>> GetFilteredPagedAsync(PaginationRequest p, NodeFilterCriteria f, CancellationToken ct = default) => _real.GetFilteredPagedAsync(p, f, ct);
        public Task<IReadOnlyList<Node>> GetNodesWithApiKeyAsync(CancellationToken ct = default) => _real.GetNodesWithApiKeyAsync(ct);
        public Task<Node> AddAsync(Node node, CancellationToken ct = default) => _real.AddAsync(node, ct);
        public Task UpdateAsync(Node node, CancellationToken ct = default) => _real.UpdateAsync(node, ct);
        public Task<int> CountAsync(CancellationToken ct = default) => _real.CountAsync(ct);
    }

    /// <summary>
    /// Un nodo que late dentro del período de gracia no debe tocarse. Protege contra la
    /// corrección perezosa —quitar el token de concurrencia— que haría pasar el caso de
    /// arriba dejando la entidad expuesta a sobrescrituras ciegas.
    /// </summary>
    [Fact]
    public async Task NodoConLatidoReciente_SigueOnline()
    {
        var nodeId = await SembrarNodoOnlineConLatidoAntiguo(TimeSpan.FromSeconds(30));

        await using (var contexto = _db.NewContext())
        {
            await CrearEvaluador(contexto).EvaluateAllNodesAsync();
        }

        await using var verificacion = _db.NewContext();
        var persistido = await verificacion.Nodes.AsNoTracking().SingleAsync(n => n.Id == nodeId);

        Assert.Equal(NodeStatus.Online, persistido.Status);
    }

    // ── Study: el mismo defecto en los flujos de fusión HL7 ──────────────────

    /// <summary>
    /// Reasignar estudios de un paciente a otro —lo que hace ADT^A40— tiene que quedar
    /// escrito. Con la lectura sin rastreo, el <c>SaveChanges</c> de la fusión completa
    /// se perdía, y los estudios seguían colgando del MRN dado de baja.
    /// </summary>
    [Fact]
    public async Task EstudioReasignadoAOtroPaciente_QuedaPersistido()
    {
        const string mrnAnterior  = "MRN-ANTERIOR-001";
        const string mrnSobrevive = "MRN-SOBREVIVE-001";

        var studyId = await SembrarEstudio(mrnAnterior);

        await using (var contexto = _db.NewContext())
        {
            var repositorio = new StudyRepository(contexto);
            var unidad = new HubUnitOfWork(contexto, NullLogger<HubUnitOfWork>.Instance);

            var estudios = await repositorio.GetByPatientForUpdateAsync(null, mrnAnterior);
            foreach (var estudio in estudios)
            {
                estudio.ReassignToPatient(mrnSobrevive, "PACIENTE^SOBREVIVE");
                await repositorio.UpdateAsync(estudio);
            }

            await unidad.SaveChangesAsync();
        }

        await using var verificacion = _db.NewContext();
        var persistido = await verificacion.Studies.AsNoTracking().SingleAsync(s => s.Id == studyId);

        Assert.Equal(mrnSobrevive, persistido.PatientId);
    }

    /// <summary>
    /// La lectura rastreada tiene que devolver exactamente los mismos estudios que la de
    /// sólo lectura a la que reemplaza. Un filtro que no coincide convertiría el arreglo
    /// en una fusión que deja estudios atrás, que es peor que el bug original.
    /// </summary>
    [Fact]
    public async Task LecturaRastreadaPorMrn_DevuelveLosMismosQueLaDeSoloLectura()
    {
        const string mrn = "MRN-PARIDAD-001";
        await SembrarEstudio(mrn);
        await SembrarEstudio(mrn);
        await SembrarEstudio("MRN-OTRO-001");

        await using var contexto = _db.NewContext();
        var repositorio = new StudyRepository(contexto);

        var soloLectura = await repositorio.GetByPatientIdAsync(mrn);
        var rastreada   = await repositorio.GetByPatientForUpdateAsync(null, mrn);

        Assert.Equal(
            soloLectura.Select(s => s.Id).OrderBy(id => id),
            rastreada.Select(s => s.Id).OrderBy(id => id));
    }

    // ── La guarda: que el próximo no reintroduzca el patrón en silencio ──────

    /// <summary>
    /// Una entidad desconectada que llega a <c>UpdateAsync</c> debe fallar en el acto y
    /// decir por qué. Es la diferencia entre descubrirlo en la primera prueba y
    /// descubrirlo leyendo un log de producción con 751 errores que culpan a una
    /// concurrencia inexistente.
    /// </summary>
    [Fact]
    public async Task NodoDesconectado_EnUpdateAsync_FallaDeInmediato()
    {
        var nodeId = await SembrarNodoOnlineConLatidoAntiguo(TimeSpan.FromHours(7));

        await using var contexto = _db.NewContext();
        var repositorio = new NodeRepository(contexto);

        // AsNoTracking: exactamente la forma en que el evaluador leía antes.
        var desconectado = await contexto.Nodes.AsNoTracking().SingleAsync(n => n.Id == nodeId);
        desconectado.MarkOffline();

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repositorio.UpdateAsync(desconectado));

        Assert.Contains("desconectada", excepcion.Message);
        Assert.Contains("GetAllForUpdateAsync", excepcion.Message);
    }

    /// <summary>La misma guarda para <c>Study</c>, que comparte el defecto y el token.</summary>
    [Fact]
    public async Task EstudioDesconectado_EnUpdateAsync_FallaDeInmediato()
    {
        var studyId = await SembrarEstudio("MRN-GUARDA-001");

        await using var contexto = _db.NewContext();
        var repositorio = new StudyRepository(contexto);

        var desconectado = await contexto.Studies.AsNoTracking().SingleAsync(s => s.Id == studyId);
        desconectado.ReassignToPatient("MRN-GUARDA-002", "OTRO^PACIENTE");

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repositorio.UpdateAsync(desconectado));

        Assert.Contains("GetByPatientForUpdateAsync", excepcion.Message);
    }

    /// <summary>
    /// Reproduce el fallo original sin la guarda, contra EF directamente: deja constancia
    /// de que la premisa del diagnóstico —adjuntar desconectado rompe el token— es real y
    /// no una teoría sobre el log. Si EF cambiara este comportamiento, esta prueba se pone
    /// roja y avisa que el resto del archivo dejó de tener sentido.
    /// </summary>
    [Fact]
    public async Task AdjuntarDesconectadoYMutarElToken_NoAfectaNingunaFila()
    {
        var nodeId = await SembrarNodoOnlineConLatidoAntiguo(TimeSpan.FromHours(7));

        await using var contexto = _db.NewContext();
        var desconectado = await contexto.Nodes.AsNoTracking().SingleAsync(n => n.Id == nodeId);

        desconectado.MarkOffline();          // muta UpdatedAt, que es el token
        contexto.Nodes.Update(desconectado); // original = actual ⇒ WHERE con el valor nuevo

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contexto.SaveChangesAsync());
    }

    // ── Sembrado ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Deja un nodo habilitado, <c>Online</c>, con el último latido a la antigüedad pedida.
    /// El latido se retrasa por SQL directo porque <c>LastHeartbeatAt</c> no tiene setter
    /// público —y no debería tenerlo—: lo que interesa es el estado de la fila, no cómo
    /// llegó a él.
    /// </summary>
    private async Task<string> SembrarNodoOnlineConLatidoAntiguo(TimeSpan antiguedad)
    {
        await using var contexto = _db.NewContext();

        var nodo = Node.Create(
            name: $"NODO-{Guid.NewGuid():N}"[..20],
            aeTitle: AeTitle.Create($"AE{Random.Shared.Next(100000, 999999)}"),
            ipAddress: "127.0.0.1",
            port: 104);

        nodo.MarkOnline();
        contexto.Nodes.Add(nodo);
        await contexto.SaveChangesAsync();

        var latido = DateTime.UtcNow - antiguedad;
        await contexto.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE nodes SET last_heartbeat_at = {latido} WHERE id = {nodo.Id}");

        return nodo.Id;
    }

    private async Task<string> SembrarEstudio(string mrn)
    {
        await using var contexto = _db.NewContext();

        var estudio = Study.Create(
            studyInstanceUid: DicomUid.Create($"1.2.826.0.1.{Random.Shared.Next(100000, 999999)}.{Random.Shared.Next(100000, 999999)}"),
            patientId: mrn,
            patientName: "PACIENTE^ANTERIOR");

        contexto.Studies.Add(estudio);
        await contexto.SaveChangesAsync();

        return estudio.Id;
    }

    // ── Cableado ─────────────────────────────────────────────────────────────

    private static NodeHealthEvaluator CrearEvaluador(HubDbContext contexto) =>
        new(new NodeRepository(contexto),
            new HealthCheckRepository(contexto),
            new HubUnitOfWork(contexto, NullLogger<HubUnitOfWork>.Instance),
            Options.Create(new NodeHealthThresholds()),
            NullLogger<NodeHealthEvaluator>.Instance);
}
