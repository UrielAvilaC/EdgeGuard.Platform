using Dicom.Edge.Contracts.Configuration;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Hub.Persistence.Context;
using Dicom.Edge.Hub.Persistence.Repositories;
using Dicom.Edge.Hub.Persistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Dicom.Edge.Hub.Tests;

/// <summary>
/// Fija que el AE title del nodo tiene <b>una sola fuente</b>: el ajuste
/// <c>dicom.ae_title</c>.
///
/// <para>De ese ajuste deriva el nodo su AE de SCP, el Calling AE con el que sale hacia
/// los PACS y el que reporta al registrarse — es el que gobierna la asociación DICOM. La
/// columna <c>nodes.ae_title</c> existe sólo para que el catálogo liste, busque y ordene
/// sin ir a los perfiles, y para que el índice único impida dos nodos con el mismo AE.</para>
///
/// <para>El defecto que motiva estas pruebas: esa columna se fijaba en el alta y nadie
/// volvía a escribirla. <c>Node.AeTitle</c> no tenía mutador y <c>UpdateNodeRequest</c> no
/// la incluía, así que quedaba congelada en el AE del primer registro. Al cambiar el AE de
/// verdad, el catálogo seguía mostrando el viejo: dos valores para el mismo nodo en dos
/// pantallas, y el incorrecto era el más visible.</para>
/// </summary>
public class NodeAeTitleMirrorTests : IClassFixture<HubSqliteFixture>
{
    private readonly HubSqliteFixture _db;

    public NodeAeTitleMirrorTests(HubSqliteFixture db) => _db = db;

    /// <summary>
    /// El caso reportado: cambiar el ajuste tiene que arrastrar el catálogo. Se verifica
    /// releyendo la fila, que es donde el defecto vivía.
    /// </summary>
    [Fact]
    public async Task AlCambiarElAjuste_LaColumnaDelCatalogoLoSigue()
    {
        var nodeId = await SembrarNodo("AE_INICIAL");

        await using (var contexto = _db.NewContext())
        {
            await CrearServicio(contexto)
                .UpdateSettingAsync(nodeId, SharedNodeSettingKeys.Dicom.AeTitle, "AE_NUEVO");
        }

        await using var verificacion = _db.NewContext();
        var nodo = await verificacion.Nodes.AsNoTracking().SingleAsync(n => n.Id == nodeId);

        Assert.Equal("AE_NUEVO", nodo.AeTitle.Value);
    }

    /// <summary>
    /// Tras el cambio, las dos pantallas tienen que coincidir. Es la condición que el
    /// operador observa, expresada directamente.
    /// </summary>
    [Fact]
    public async Task TrasElCambio_CatalogoYConfiguracionCoinciden()
    {
        var nodeId = await SembrarNodo("AE_ORIGEN");

        await using (var contexto = _db.NewContext())
        {
            await CrearServicio(contexto)
                .UpdateSettingAsync(nodeId, SharedNodeSettingKeys.Dicom.AeTitle, "AE_UNIFICADO");
        }

        await using var verificacion = _db.NewContext();
        var servicio = CrearServicio(verificacion);

        var enCatalogo = (await verificacion.Nodes.AsNoTracking().SingleAsync(n => n.Id == nodeId)).AeTitle.Value;
        var enConfiguracion = await servicio.GetSettingValueAsync(nodeId, SharedNodeSettingKeys.Dicom.AeTitle);

        Assert.Equal(enConfiguracion, enCatalogo);
    }

    /// <summary>
    /// Dos nodos no pueden compartir AE: colisionarían en las asociaciones DICOM y el
    /// índice único rechazaría la escritura. El espejo se salta ese caso en vez de tumbar
    /// el guardado del ajuste, que es la fuente y ya quedó escrito.
    /// </summary>
    [Fact]
    public async Task SiElAeYaEsDeOtroNodo_ElEspejoNoPisaYElAjusteSeGuarda()
    {
        await SembrarNodo("AE_OCUPADO");
        var segundo = await SembrarNodo("AE_LIBRE");

        await using (var contexto = _db.NewContext())
        {
            var ok = await CrearServicio(contexto)
                .UpdateSettingAsync(segundo, SharedNodeSettingKeys.Dicom.AeTitle, "AE_OCUPADO");

            Assert.True(ok);
        }

        await using var verificacion = _db.NewContext();
        var nodo = await verificacion.Nodes.AsNoTracking().SingleAsync(n => n.Id == segundo);

        // La columna no se pisó — habría violado el índice único.
        Assert.Equal("AE_LIBRE", nodo.AeTitle.Value);

        // Pero el ajuste sí quedó guardado: es la fuente, y el operador debe resolver el choque.
        var enConfiguracion = await CrearServicio(verificacion)
            .GetSettingValueAsync(segundo, SharedNodeSettingKeys.Dicom.AeTitle);
        Assert.Equal("AE_OCUPADO", enConfiguracion);
    }

    /// <summary>
    /// Un valor inválido no puede dejar la columna a medias ni reventar el guardado.
    /// </summary>
    [Fact]
    public async Task AjusteConAeInvalido_DejaLaColumnaIntacta()
    {
        var nodeId = await SembrarNodo("AE_BUENO");

        await using (var contexto = _db.NewContext())
        {
            await CrearServicio(contexto).UpdateSettingAsync(
                nodeId, SharedNodeSettingKeys.Dicom.AeTitle, "ESTE_AE_ES_DEMASIADO_LARGO_PARA_DICOM");
        }

        await using var verificacion = _db.NewContext();
        var nodo = await verificacion.Nodes.AsNoTracking().SingleAsync(n => n.Id == nodeId);

        Assert.Equal("AE_BUENO", nodo.AeTitle.Value);
    }

    /// <summary>
    /// Cambiar cualquier otro ajuste no debe tocar el AE del catálogo.
    /// </summary>
    [Fact]
    public async Task OtroAjuste_NoMueveElAeDelCatalogo()
    {
        var nodeId = await SembrarNodo("AE_ESTABLE");

        await using (var contexto = _db.NewContext())
        {
            await CrearServicio(contexto)
                .UpdateSettingAsync(nodeId, SharedNodeSettingKeys.Dicom.Port, "11113");
        }

        await using var verificacion = _db.NewContext();
        var nodo = await verificacion.Nodes.AsNoTracking().SingleAsync(n => n.Id == nodeId);

        Assert.Equal("AE_ESTABLE", nodo.AeTitle.Value);
    }

    /// <summary>
    /// Las dos claves de AE que no tenían ningún efecto —<c>node.ae_title</c> en General y
    /// <c>sender.local_ae_title</c> en PacsSender— ya no se ofrecen. Eran editables en la
    /// pantalla de configuración pero el nodo las ignoraba a propósito, así que el operador
    /// veía tres AE title y sólo uno hacía algo.
    /// </summary>
    [Fact]
    public void ElCatalogoDeAjustes_ExponeUnSoloAeTitlePropio()
    {
        var claves = SharedNodeSettingDefaults.All.Select(d => d.Key).ToList();

        Assert.Contains(SharedNodeSettingKeys.Dicom.AeTitle, claves);
        Assert.DoesNotContain(SharedNodeSettingKeys.General.AeTitle, claves);
        Assert.DoesNotContain(SharedNodeSettingKeys.PacsSender.LocalAeTitle, claves);

        // dicom.allowed_ae_titles se queda: es la lista blanca de AEs remotos que pueden
        // asociarse contra el nodo, no el AE propio del nodo. Nombres parecidos, cosas
        // distintas — conviene dejarlo escrito para que nadie lo retire de arrastre.
        Assert.Contains(SharedNodeSettingKeys.Dicom.AllowedAeTitles, claves);
    }

    // ── Cableado ─────────────────────────────────────────────────────────────

    private static NodeConfigurationService CrearServicio(HubDbContext contexto) =>
        new(new NodeConfigurationProfileRepository(contexto),
            new NodeRepository(contexto),
            new HubUnitOfWork(contexto, NullLogger<HubUnitOfWork>.Instance),
            NullLogger<NodeConfigurationService>.Instance);

    private async Task<string> SembrarNodo(string aeTitle)
    {
        await using var contexto = _db.NewContext();

        var nodo = Node.Create(
            name: $"NODO-{Guid.NewGuid():N}"[..20],
            aeTitle: AeTitle.Create(aeTitle),
            ipAddress: "127.0.0.1",
            port: 104);

        contexto.Nodes.Add(nodo);
        await contexto.SaveChangesAsync();

        // Los perfiles nacen con el mismo AE, igual que en el alta real.
        await CrearServicio(contexto).InitializeNodeDefaultsAsync(
            nodo.Id,
            new Dictionary<string, string> { [SharedNodeSettingKeys.Dicom.AeTitle] = aeTitle });

        return nodo.Id;
    }
}
