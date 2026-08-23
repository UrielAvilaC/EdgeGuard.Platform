using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Abstractions.Storage;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Node.Persistence.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Node.Configuration;

/// <summary>
/// Envía periódicamente al Hub la medición de almacenamiento del nodo.
///
/// <para>Este servicio no existía. El Hub expone el endpoint de reporte de salud
/// desde siempre y lo persiste en su histórico, pero ningún cliente lo llamaba
/// nunca: el nodo medía su consumo para decidir purgas y descartaba el número.
/// Ese era el eslabón que dejaba el Dashboard en 0%.</para>
///
/// <para>Va por su propio intervalo y no colgado del latido, porque medir cuesta
/// y probar que se está vivo no debería costar nada.</para>
/// </summary>
public sealed class HubHealthReportHostedService(
    IHubSyncClient hubClient,
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<HubConnectionOptions> optionsMonitor,
    ILogger<HubHealthReportHostedService> logger) : BackgroundService
{
    private HubConnectionOptions Opts => optionsMonitor.CurrentValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Opts.Enabled)
        {
            logger.LogInformation("Health reporting disabled — Hub connection is off");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(30, Opts.HealthReportIntervalSeconds));
        logger.LogInformation("Health report service started — interval={Seconds}s", interval.TotalSeconds);

        // La primera vuelta espera: al arrancar, el nodo aún no se ha registrado
        // y la medición no tendría a dónde ir.
        using var timer = new PeriodicTimer(interval);

        while (await SafeWaitAsync(timer, stoppingToken))
        {
            try
            {
                await ReportOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Un reporte perdido no es motivo para tumbar el servicio: el
                // siguiente ciclo lo intenta de nuevo con datos más frescos.
                logger.LogWarning(ex, "Health report cycle failed");
            }
        }
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }

    private async Task ReportOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var probe = scope.ServiceProvider.GetRequiredService<IStorageUsageProbe>();
        var settings = scope.ServiceProvider.GetRequiredService<INodeSettingsService>();

        var nodeId = hubClient.RegisteredNodeId;
        if (string.IsNullOrEmpty(nodeId))
        {
            logger.LogDebug("Skipping health report -- node not yet registered");
            return;
        }

        var usage = await probe.MeasureAsync(ct);
        var appliedVersion = await settings.GetAsync<string>(NodeSettingKeys.System.ConfigVersion, string.Empty, ct);

        var request = new NodeHealthReportRequest
        {
            NodeId = nodeId,
            StorageDicomMb = usage.DicomMb,
            StorageDatabaseMb = usage.DatabaseMb,
            StorageVolumeFreeMb = usage.VolumeFreeMb,
            StorageVolumeTotalMb = usage.VolumeTotalMb,

            // 0 en la configuración significa "sin límite"; hacia el Hub eso se
            // expresa como ausencia de valor, para que no se pinte una barra
            // contra un límite que no existe.
            StorageLimitMb = usage.LimitMb > 0 ? usage.LimitMb : null,
            StorageMeasuredAt = usage.MeasuredAt,
            AppliedConfigVersion = appliedVersion,
        };

        await hubClient.SendHealthReportAsync(request, ct);
    }
}
