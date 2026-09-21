using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Hub.Persistence.Context;
using Dicom.Edge.Hub.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dicom.Edge.Hub.Tests;

/// <summary>
/// Fija que una entidad eliminada lógicamente <b>deja de existir para todo el Hub</b>.
///
/// <para>El riesgo del borrado lógico no es borrar de más: es borrar de mentira. Una fila
/// que sigue ahí y que algún camino todavía encuentra es peor que no haber ofrecido el
/// borrado, porque el operador cree que la retiró.</para>
///
/// <para>El camino que más importa cubrir es resolver por clave: por ahí pasan el latido,
/// el reporte de salud y el ruteo HL7. Un nodo eliminado que siguiera resolviéndose por id
/// quedaría invisible en los listados y vivo en la operación, que es la peor combinación.</para>
/// </summary>
public class SoftDeleteFilterTests : IClassFixture<HubSqliteFixture>
{
    private readonly HubSqliteFixture _db;

    public SoftDeleteFilterTests(HubSqliteFixture db) => _db = db;

    /// <summary>
    /// Fija una garantía de la versión de EF, no del código propio: que <c>FindAsync</c>
    /// aplica los filtros globales.
    ///
    /// <para>No siempre fue así — en versiones anteriores <c>Find</c> los ignoraba, y los
    /// repositorios de aquí resuelven por clave con <c>Find</c> precisamente porque
    /// consulta antes el change tracker y se ahorra el viaje a la base. Esa optimización
    /// sólo es segura mientras la garantía se cumpla.</para>
    ///
    /// <para>Si una actualización de EF la revirtiera, el síntoma sería silencioso: nodos y
    /// PACS eliminados respondiendo con normalidad a todo lo que los busca por id. Esta
    /// prueba lo convierte en un fallo de compilación del pipeline.</para>
    /// </summary>
    [Fact]
    public async Task FindAsync_RespetaElFiltroDeBorradoLogico()
    {
        var nodeId = await SembrarNodo("AE_FIND_PROBE");
        await EliminarNodo(nodeId);

        await using var contexto = _db.NewContext();

        Assert.Null(await contexto.Nodes.FindAsync([nodeId]));
        Assert.NotNull(await contexto.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == nodeId));
    }

    // ── Nodos ────────────────────────────────────────────────────────────────

    /// <summary>
    /// La prueba que justifica el cambio de <c>FindAsync</c> a <c>FirstOrDefaultAsync</c>.
    /// Resolver por id es el camino del latido y del ruteo: si ahí sigue apareciendo, el
    /// nodo no está eliminado en ningún sentido útil.
    /// </summary>
    [Fact]
    public async Task NodoEliminado_NoSeResuelvePorId()
    {
        var nodeId = await SembrarNodo("AE_NODO_DEL");
        await EliminarNodo(nodeId);

        await using var contexto = _db.NewContext();
        var encontrado = await new NodeRepository(contexto).GetByIdAsync(nodeId);

        Assert.Null(encontrado);
    }

    [Fact]
    public async Task NodoEliminado_DesapareceDeTodosLosListados()
    {
        var nodeId = await SembrarNodo("AE_NODO_LIST");
        await EliminarNodo(nodeId);

        await using var contexto = _db.NewContext();
        var repo = new NodeRepository(contexto);

        Assert.DoesNotContain(await repo.GetAllAsync(),           n => n.Id == nodeId);
        Assert.DoesNotContain(await repo.GetAllForUpdateAsync(),  n => n.Id == nodeId);
        Assert.DoesNotContain(await repo.GetActiveNodesAsync(),   n => n.Id == nodeId);
        Assert.DoesNotContain(await repo.GetNodesWithApiKeyAsync(), n => n.Id == nodeId);
    }

    /// <summary>
    /// El re-registro busca por nombre + IP. Si encontrara el nodo eliminado, lo
    /// resucitaría por la puerta de atrás sin que nadie lo decidiera.
    /// </summary>
    [Fact]
    public async Task NodoEliminado_NoSeEncuentraPorNombreEIp()
    {
        var nodeId = await SembrarNodo("AE_NODO_REG");

        string nombre;
        await using (var lectura = _db.NewContext())
            nombre = (await lectura.Nodes.AsNoTracking().SingleAsync(n => n.Id == nodeId)).Name;

        await EliminarNodo(nodeId);

        await using var contexto = _db.NewContext();
        var encontrado = await new NodeRepository(contexto).GetByNameAndIpAsync(nombre, "127.0.0.1");

        Assert.Null(encontrado);
    }

    /// <summary>
    /// La fila sigue ahí. Es el punto entero del borrado lógico: los estudios guardan de
    /// qué nodo llegaron, y ese rastro no puede romperse.
    /// </summary>
    [Fact]
    public async Task NodoEliminado_ConservaSuFilaYSuHistorial()
    {
        var nodeId = await SembrarNodo("AE_NODO_HIST");
        await EliminarNodo(nodeId);

        await using var contexto = _db.NewContext();
        var fila = await contexto.Nodes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(n => n.Id == nodeId);

        Assert.True(fila.IsDeleted);
        Assert.NotNull(fila.DeletedAt);
    }

    /// <summary>
    /// El AE de un nodo eliminado queda libre. Sin el índice único <b>parcial</b>, volver
    /// a dar de alta el mismo sitio —el caso normal tras reinstalarlo— fallaría con una
    /// violación de índice contra un nodo que ya nadie ve.
    /// </summary>
    [Fact]
    public async Task TrasEliminarUnNodo_SuAeVuelveAEstarDisponible()
    {
        const string ae = "AE_REUTILIZA";

        var primero = await SembrarNodo(ae);
        await EliminarNodo(primero);

        var excepcion = await Record.ExceptionAsync(() => SembrarNodo(ae));

        Assert.Null(excepcion);
    }

    // ── PACS ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// El borrado de PACS pasó de físico a lógico. Antes la fila desaparecía y los
    /// estudios quedaban con <c>TargetPacsId</c> apuntando a la nada.
    /// </summary>
    [Fact]
    public async Task PacsEliminado_ConservaSuFilaPeroSaleDelCatalogo()
    {
        var pacsId = await SembrarPacs("AE_PACS_DEL");
        await EliminarPacs(pacsId);

        await using var contexto = _db.NewContext();

        Assert.Null(await new PacsServerRepository(contexto).GetByIdAsync(pacsId));
        Assert.DoesNotContain(await new PacsServerRepository(contexto).GetAllAsync(), p => p.Id == pacsId);

        var fila = await contexto.PacsServers
            .IgnoreQueryFilters().AsNoTracking().SingleAsync(p => p.Id == pacsId);

        Assert.True(fila.IsDeleted);
        Assert.NotNull(fila.DeletedAt);
    }

    /// <summary>
    /// Un PACS global eliminado no puede seguir heredándose a los nodos nuevos.
    /// </summary>
    [Fact]
    public async Task PacsGlobalEliminado_YaNoSeHereda()
    {
        var pacsId = await SembrarPacs("AE_PACS_GLOB", global: true);
        await EliminarPacs(pacsId);

        await using var contexto = _db.NewContext();
        var globales = await new PacsServerRepository(contexto).GetGlobalAsync();

        Assert.DoesNotContain(globales, p => p.Id == pacsId);
    }

    [Fact]
    public async Task TrasEliminarUnPacs_SuAeVuelveAEstarDisponible()
    {
        const string ae = "AE_PACS_REUSO";

        var primero = await SembrarPacs(ae);
        await EliminarPacs(primero);

        var excepcion = await Record.ExceptionAsync(() => SembrarPacs(ae));

        Assert.Null(excepcion);
    }

    /// <summary>
    /// Eliminar un PACS lo retira de los nodos que lo tenían asignado. Las asignaciones no
    /// son clave foránea, así que nadie las limpia solo: sin esto el nodo quedaría
    /// apuntando a un destino que ya no existe y seguiría intentando enviarle.
    /// </summary>
    [Fact]
    public async Task PacsEliminado_SeRetiraDeLosNodosQueLoTenian()
    {
        var pacsId = await SembrarPacs("AE_PACS_ASIG");
        var nodeId = await SembrarNodo("AE_NODO_ASIG");

        await using (var contexto = _db.NewContext())
        {
            var repo = new NodeRepository(contexto);
            var nodo = await repo.GetWithPacsAssignmentsAsync(nodeId);
            nodo!.AssignPacs(pacsId);
            await repo.UpdateAsync(nodo);
            await contexto.SaveChangesAsync();
        }

        await EliminarPacsRetirandoDeNodos(pacsId);

        await using var verificacion = _db.NewContext();
        var resultado = await new NodeRepository(verificacion).GetWithPacsAssignmentsAsync(nodeId);

        Assert.DoesNotContain(resultado!.PacsAssignments, a => a.PacsId == pacsId && a.IsActive);
    }

    // ── Sembrado y operaciones ───────────────────────────────────────────────

    private async Task<string> SembrarNodo(string aeTitle)
    {
        await using var contexto = _db.NewContext();

        var nodo = Node.Create(
            name: $"NODO-{Guid.NewGuid():N}"[..20],
            aeTitle: AeTitle.Create(aeTitle),
            ipAddress: "127.0.0.1",
            port: 104);

        nodo.SetApiKey("hash", "secret");
        contexto.Nodes.Add(nodo);
        await contexto.SaveChangesAsync();
        return nodo.Id;
    }

    private async Task EliminarNodo(string nodeId)
    {
        await using var contexto = _db.NewContext();
        var repo = new NodeRepository(contexto);

        var nodo = await repo.GetByIdAsync(nodeId);
        nodo!.SoftDelete();
        await repo.UpdateAsync(nodo);
        await contexto.SaveChangesAsync();
    }

    private async Task<string> SembrarPacs(string aeTitle, bool global = false)
    {
        await using var contexto = _db.NewContext();

        var pacs = PacsServer.Create(
            name: $"PACS-{Guid.NewGuid():N}"[..20],
            aeTitle: AeTitle.Create(aeTitle),
            hostName: "127.0.0.1",
            port: 104,
            isGlobal: global);

        contexto.PacsServers.Add(pacs);
        await contexto.SaveChangesAsync();
        return pacs.Id;
    }

    private async Task EliminarPacs(string pacsId)
    {
        await using var contexto = _db.NewContext();
        await new PacsServerRepository(contexto).DeleteAsync(pacsId);
        await contexto.SaveChangesAsync();
    }

    /// <summary>Replica lo que hace el servicio: desasignar y luego retirar.</summary>
    private async Task EliminarPacsRetirandoDeNodos(string pacsId)
    {
        await using var contexto = _db.NewContext();
        var nodeRepo = new NodeRepository(contexto);

        foreach (var nodo in await nodeRepo.GetAllForUpdateAsync())
        {
            var conAsignaciones = await nodeRepo.GetWithPacsAssignmentsAsync(nodo.Id);
            if (conAsignaciones?.PacsAssignments.Any(a => a.PacsId == pacsId && a.IsActive) != true) continue;

            conAsignaciones.UnassignPacs(pacsId);
            await nodeRepo.UpdateAsync(conAsignaciones);
        }

        await new PacsServerRepository(contexto).DeleteAsync(pacsId);
        await contexto.SaveChangesAsync();
    }
}
