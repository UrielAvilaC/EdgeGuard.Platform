using Dicom.Edge.Diagnostics.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Diagnostics.HealthChecks;

/// <summary>
/// Checks local storage availability and free space against configured thresholds.
/// Reports degraded when storage is below the minimum threshold.
/// </summary>
public sealed class StorageHealthCheck : IHealthCheck
{
    private readonly long _minAvailableBytes;
    private readonly string _storagePath;

    public StorageHealthCheck(IOptions<HealthCheckThresholdOptions> healthOptions)
    {
        var opts = healthOptions.Value;
        _minAvailableBytes = opts.StorageMinAvailableMb * 1024 * 1024;
        _storagePath = string.IsNullOrWhiteSpace(opts.StoragePath)
            ? global::System.Environment.CurrentDirectory
            : opts.StoragePath;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.GetFullPath(_storagePath);
            var root = Path.GetPathRoot(fullPath);

            if (string.IsNullOrEmpty(root))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Cannot determine storage root path.",
                    data: new Dictionary<string, object> { ["ConfiguredPath"] = _storagePath }));
            }

            var driveInfo = new DriveInfo(root);

            if (!driveInfo.IsReady)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Drive {root} is not ready."));
            }

            var availableBytes = driveInfo.AvailableFreeSpace;
            var totalBytes = driveInfo.TotalSize;
            var usedPercent = (double)(totalBytes - availableBytes) / totalBytes * 100;

            var data = new Dictionary<string, object>
            {
                ["AvailableMB"] = availableBytes / (1024 * 1024),
                ["TotalMB"] = totalBytes / (1024 * 1024),
                ["UsedPercent"] = Math.Round(usedPercent, 2),
                ["Path"] = fullPath,
                ["ThresholdMB"] = _minAvailableBytes / (1024 * 1024)
            };

            if (availableBytes < _minAvailableBytes)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Storage is low: {availableBytes / (1024 * 1024)} MB available " +
                    $"(threshold: {_minAvailableBytes / (1024 * 1024)} MB).",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Storage OK: {availableBytes / (1024 * 1024)} MB available.",
                data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Storage health check failed.", ex));
        }
    }
}
