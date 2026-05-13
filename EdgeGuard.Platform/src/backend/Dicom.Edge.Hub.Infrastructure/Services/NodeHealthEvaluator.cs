using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Services;
using Dicom.Edge.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Evaluates node health based on heartbeat timing and metrics.
/// </summary>
public class NodeHealthEvaluator : INodeHealthEvaluator
{
    private readonly INodeRepository _nodeRepository;
    private readonly IHealthCheckRepository _healthCheckRepository;
    private readonly ILogger<NodeHealthEvaluator> _logger;

    public NodeHealthEvaluator(
        INodeRepository nodeRepository,
        IHealthCheckRepository healthCheckRepository,
        ILogger<NodeHealthEvaluator> logger)
    {
        _nodeRepository = nodeRepository;
        _healthCheckRepository = healthCheckRepository;
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
    }

    private async Task EvaluateNodeInternalAsync(Node node, CancellationToken ct)
    {
        var latest = await _healthCheckRepository.GetLatestByNodeAsync(node.Id, ct);

        // Grace period = max(5 min, heartbeatInterval * 5).
        // Multiplier of 5 tolerates transient network issues and IIS recycles
        // without false-positive offline marking on the first missed heartbeat.
        var minGraceSec = Math.Max(300, node.HealthCheckIntervalSeconds * 5);
        var maxAllowedGap = TimeSpan.FromSeconds(minGraceSec);
        var lastSeen = node.LastHeartbeatAt ?? node.CreatedAt;
        var timeSinceLastHeartbeat = DateTime.UtcNow - lastSeen;

        if (timeSinceLastHeartbeat > maxAllowedGap && node.Status != NodeStatus.Offline)
        {
            _logger.LogWarning(
                "Node {NodeId} last heartbeat was {Seconds}s ago (threshold: {Threshold}s), marking offline",
                node.Id, timeSinceLastHeartbeat.TotalSeconds, maxAllowedGap.TotalSeconds);

            node.MarkOffline();
            await _nodeRepository.UpdateAsync(node, ct);
            return;
        }

        // Check for degraded metrics from latest healthcheck
        if (latest is not null && node.Status == NodeStatus.Online)
        {
            var isDegraded = (latest.CpuUsagePercent.HasValue && latest.CpuUsagePercent > 90) ||
                             (latest.DiskAvailableMb.HasValue && latest.DiskAvailableMb < 1024) ||
                             (latest.QueuedStudies.HasValue && latest.QueuedStudies > 100);

            if (isDegraded)
            {
                _logger.LogWarning("Node {NodeId} shows degraded metrics, marking degraded", node.Id);
                node.MarkDegraded();
                await _nodeRepository.UpdateAsync(node, ct);
            }
        }
    }
}
