using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Services;
using Dicom.Edge.Hub.Infrastructure.HostedServices;
using Dicom.Edge.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Decide el estado de cada nodo a partir de la antigüedad de su latido y de las
/// métricas de su último reporte de salud.
/// </summary>
public class NodeHealthEvaluator : INodeHealthEvaluator
{
    private readonly INodeRepository _nodeRepository;
    private readonly IHealthCheckRepository _healthCheckRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NodeHealthThresholds _thresholds;
    private readonly ILogger<NodeHealthEvaluator> _logger;

    public NodeHealthEvaluator(
        INodeRepository nodeRepository,
        IHealthCheckRepository healthCheckRepository,
        IUnitOfWork unitOfWork,
        IOptions<NodeHealthThresholds> thresholds,
        ILogger<NodeHealthEvaluator> logger)
    {
        _nodeRepository = nodeRepository;
        _healthCheckRepository = healthCheckRepository;
        _unitOfWork = unitOfWork;
        _thresholds = thresholds.Value;
        _logger = logger;
    }

    public async Task EvaluateAllNodesAsync(CancellationToken ct = default)
    {
        var nodes = await _nodeRepository.GetAllAsync(ct);

        foreach (var node in nodes)
        {
            if (!node.IsEnabled) continue;
            await EvaluateNodeInternalAsync(node, ct);
        }

        // Un solo commit al cerrar el ciclo, no uno por nodo. Antes no había
        // ninguno: el repositorio solo marca la entidad como modificada, así que
        // cada transición calculada se descartaba al liberarse el scope y los
        // nodos quedaban Online con latidos de semanas.
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task EvaluateNodeAsync(string nodeId, CancellationToken ct = default)
    {
        var node = await _nodeRepository.GetByIdAsync(nodeId, ct);
        if (node is null)
        {
            _logger.LogWarning("Node {NodeId} not found for health evaluation", nodeId);
            return;
        }

        await EvaluateNodeInternalAsync(node, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task EvaluateNodeInternalAsync(Node node, CancellationToken ct)
    {
        // Grace period = max(5 min, heartbeatInterval * 5).
        // El multiplicador tolera cortes de red pasajeros y reciclados de IIS sin
        // marcar offline al primer latido perdido.
        var minGraceSec = Math.Max(300, node.HealthCheckIntervalSeconds * 5);
        var maxAllowedGap = TimeSpan.FromSeconds(minGraceSec);
        var lastSeen = node.LastHeartbeatAt ?? node.CreatedAt;
        var timeSinceLastHeartbeat = DateTime.UtcNow - lastSeen;

        if (timeSinceLastHeartbeat > maxAllowedGap)
        {
            if (node.Status != NodeStatus.Offline)
            {
                _logger.LogWarning(
                    "Node {NodeId} last heartbeat was {Seconds:F0}s ago (threshold: {Threshold}s), marking offline",
                    node.Id, timeSinceLastHeartbeat.TotalSeconds, maxAllowedGap.TotalSeconds);

                node.MarkOffline();
                await _nodeRepository.UpdateAsync(node, ct);
            }

            return;
        }

        // Solo se evalúan métricas de nodos vivos. Incluir Degraded es lo que
        // permite el camino de vuelta: antes solo se entraba a este bloque desde
        // Online, así que un nodo degradado se quedaba así para siempre.
        if (node.Status is not (NodeStatus.Online or NodeStatus.Degraded)) return;

        var latest = await _healthCheckRepository.GetLatestByNodeAsync(node.Id, ct);
        if (latest is null) return;

        var reasons = CollectDegradedReasons(node, latest, _thresholds);

        if (reasons.Count > 0 && node.Status == NodeStatus.Online)
        {
            _logger.LogWarning(
                "Node {NodeId} degraded: {Reasons}", node.Id, string.Join("; ", reasons));

            node.MarkDegraded();
            await _nodeRepository.UpdateAsync(node, ct);
        }
        else if (reasons.Count == 0 && node.Status == NodeStatus.Degraded)
        {
            _logger.LogInformation(
                "Node {NodeId} metrics back to normal, marking online", node.Id);

            node.MarkOnline();
            await _nodeRepository.UpdateAsync(node, ct);
        }
    }

    /// <summary>
    /// Devuelve una razón por cada umbral superado. Son razones con texto en vez
    /// de un booleano porque en un incidente lo primero que se necesita saber es
    /// cuál de las tres condiciones disparó.
    /// </summary>
    internal static List<string> CollectDegradedReasons(
        Node node, HealthCheckRecord latest, NodeHealthThresholds t)
    {
        var reasons = new List<string>();

        if (latest.CpuUsagePercent > t.CpuPercent)
            reasons.Add($"CPU {latest.CpuUsagePercent:F0}% (umbral {t.CpuPercent}%)");

        if (latest.QueuedStudies > t.QueueDepth)
            reasons.Add($"cola {latest.QueuedStudies} (umbral {t.QueueDepth})");

        // El uso solo es interpretable contra un límite, y se prefiere el que el
        // nodo confirmó en este mismo reporte: uso y límite quedan medidos por la
        // misma parte en el mismo instante. El límite administrado en el Hub puede
        // llevar minutos sin llegar al nodo, y compararlo daría un porcentaje que
        // no corresponde a lo que el nodo está aplicando.
        var limitMb = latest.StorageLimitMb ?? node.StorageLimitMb;
        var usedMb = (latest.StorageDicomMb ?? 0) + (latest.StorageDatabaseMb ?? 0);

        // Las dos guardas importan: sin medición no hay nada que juzgar, y sin
        // límite el porcentaje no existe. `is > 0` cubre null y cero a la vez.
        if (latest.StorageDicomMb is not null && limitMb is > 0)
        {
            var percent = usedMb * 100.0 / limitMb.Value;
            if (percent >= t.StoragePercent)
                reasons.Add($"almacenamiento {percent:F0}% de {limitMb} MB (umbral {t.StoragePercent}%)");
        }

        return reasons;
    }
}
