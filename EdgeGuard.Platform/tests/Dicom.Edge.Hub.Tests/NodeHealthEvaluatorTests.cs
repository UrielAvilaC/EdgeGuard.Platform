using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Infrastructure.HostedServices;
using Dicom.Edge.Hub.Infrastructure.Services;
using Dicom.Edge.Models.Enums;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Xunit;

namespace Dicom.Edge.Hub.Tests;

/// <summary>
/// Cubre la regla de degradado por almacenamiento.
///
/// <para>El modelo pasó de espacio libre a espacio usado, y ese cambio tiene una
/// trampa que ningún compilador puede ver: los dos campos son <c>long?</c>, así
/// que un renombre mecánico de <c>DiskAvailableMb</c> a <c>StorageDicomMb</c>
/// deja la comparación intacta y perfectamente compilable, pero invertida —
/// pasa de "menos de 1 GB libre" a "menos de 1 GB usado". Un nodo vacío se
/// marcaría degradado y uno lleno se vería sano.</para>
///
/// <para>Estas pruebas son lo único que lo detecta.</para>
/// </summary>
public class NodeHealthEvaluatorTests
{
    private static readonly NodeHealthThresholds Umbrales = new();

    private static Node NodoDePrueba(long? limitMb = null)
    {
        var node = Node.Create(
            name: "NODO-PRUEBA",
            aeTitle: AeTitle.Create("TEST_AE"),
            ipAddress: "127.0.0.1",
            port: 104,
            storageLimitMb: limitMb);

        return node;
    }

    private static HealthCheckRecord Reporte(
        long? dicomMb = null,
        long? databaseMb = null,
        long? limitMb = null,
        double? cpuPercent = null,
        int? queuedStudies = null)
        => HealthCheckRecord.Create(
            nodeId: "n1",
            reportedStatus: NodeStatus.Online,
            cpuUsagePercent: cpuPercent,
            storageDicomMb: dicomMb,
            storageDatabaseMb: databaseMb,
            storageLimitMb: limitMb,
            queuedStudies: queuedStudies);

    /// <summary>
    /// El canario. Con la regla invertida este es el caso que se pone rojo: un
    /// nodo prácticamente vacío, con cuota amplia, jamás debe degradarse.
    /// </summary>
    [Fact]
    public void NodoVacioConCuotaAmplia_NoSeDegrada()
    {
        var razones = NodeHealthEvaluator.CollectDegradedReasons(
            NodoDePrueba(), Reporte(dicomMb: 0, databaseMb: 0, limitMb: 10_000), Umbrales);

        Assert.Empty(razones);
    }

    [Fact]
    public void NodoCercaDeLaCuota_SeDegrada()
    {
        var razones = NodeHealthEvaluator.CollectDegradedReasons(
            NodoDePrueba(), Reporte(dicomMb: 9_400, databaseMb: 100, limitMb: 10_000), Umbrales);

        Assert.Contains(razones, r => r.Contains("almacenamiento", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NodoHolgadoDentroDeLaCuota_NoSeDegrada()
    {
        var razones = NodeHealthEvaluator.CollectDegradedReasons(
            NodoDePrueba(), Reporte(dicomMb: 1_000, databaseMb: 50, limitMb: 10_000), Umbrales);

        Assert.Empty(razones);
    }

    /// <summary>
    /// Sin cuota no hay porcentaje, así que la regla no participa por muy grande
    /// que sea el consumo. Es deliberado: un nodo sin cuota no está incumpliendo
    /// nada.
    /// </summary>
    [Fact]
    public void SinCuotaConfigurada_LaReglaNoParticipa()
    {
        var razones = NodeHealthEvaluator.CollectDegradedReasons(
            NodoDePrueba(limitMb: null), Reporte(dicomMb: 500_000, databaseMb: 0), Umbrales);

        Assert.Empty(razones);
    }

    /// <summary>Una cuota de cero también significa "sin límite", no "lleno".</summary>
    [Fact]
    public void CuotaEnCero_LaReglaNoParticipa()
    {
        var razones = NodeHealthEvaluator.CollectDegradedReasons(
            NodoDePrueba(), Reporte(dicomMb: 9_999, databaseMb: 0, limitMb: 0), Umbrales);

        Assert.Empty(razones);
    }

    /// <summary>
    /// Un nodo que nunca reportó no tiene medición que juzgar. Marcarlo degradado
    /// por ausencia de datos sería inventar información.
    /// </summary>
    [Fact]
    public void SinMedicion_LaReglaNoParticipa()
    {
        var razones = NodeHealthEvaluator.CollectDegradedReasons(
            NodoDePrueba(limitMb: 10_000), Reporte(dicomMb: null, databaseMb: null), Umbrales);

        Assert.Empty(razones);
    }

    /// <summary>
    /// El eco del nodo gana sobre el límite administrado: uso y cuota quedan
    /// medidos por la misma parte en el mismo instante. Si el Hub acaba de bajar
    /// la cuota y el push no ha llegado, el nodo todavía aplica la anterior y es
    /// esa la que describe su situación real.
    /// </summary>
    [Fact]
    public void ElLimiteDelReporteGanaSobreElAdministrado()
    {
        // Administrado: 1.000 MB (usado sería 950 = 95%, degradaría).
        // Reportado por el nodo: 100.000 MB (usado sería 0,95%, no degrada).
        var razones = NodeHealthEvaluator.CollectDegradedReasons(
            NodoDePrueba(limitMb: 1_000),
            Reporte(dicomMb: 950, databaseMb: 0, limitMb: 100_000),
            Umbrales);

        Assert.Empty(razones);
    }

    [Fact]
    public void CadaUmbralSuperadoAportaSuPropiaRazon()
    {
        var razones = NodeHealthEvaluator.CollectDegradedReasons(
            NodoDePrueba(),
            Reporte(dicomMb: 9_900, databaseMb: 0, limitMb: 10_000, cpuPercent: 99, queuedStudies: 500),
            Umbrales);

        // Tres razones distintas: en un incidente lo primero que se necesita
        // saber es cuál de las condiciones disparó.
        Assert.Equal(3, razones.Count);
    }
}
