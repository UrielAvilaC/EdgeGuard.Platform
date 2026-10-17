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
    private readonly string _configuredPath;
    private readonly IStorageHealthLocation _location;

    public StorageHealthCheck(
        IOptions<HealthCheckThresholdOptions> healthOptions,
        IStorageHealthLocation location)
    {
        var opts = healthOptions.Value;
        _minAvailableBytes = opts.StorageMinAvailableMb * 1024 * 1024;
        _configuredPath = string.IsNullOrWhiteSpace(opts.StoragePath)
            ? global::System.Environment.CurrentDirectory
            : opts.StoragePath;
        _location = location;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // La ruta se resuelve en cada chequeo: puede cambiar en caliente y el valor
        // configurado es solo el respaldo para cuando no hay forma de averiguarla.
        string storagePath;
        try
        {
            storagePath = await _location.GetPathAsync(cancellationToken) is { Length: > 0 } path
                ? path
                : _configuredPath;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            storagePath = _configuredPath;
        }

        try
        {
            var fullPath = Path.GetFullPath(storagePath);
            var root = Path.GetPathRoot(fullPath);

            if (string.IsNullOrEmpty(root))
            {
                return HealthCheckResult.Unhealthy(
                    "Cannot determine storage root path.",
                    data: new Dictionary<string, object> { ["ConfiguredPath"] = storagePath });
            }

            var driveInfo = new DriveInfo(root);

            if (!driveInfo.IsReady)
            {
                return HealthCheckResult.Unhealthy($"Drive {root} is not ready.");
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
                return HealthCheckResult.Degraded(
                    $"Storage is low: {availableBytes / (1024 * 1024)} MB available " +
                    $"(threshold: {_minAvailableBytes / (1024 * 1024)} MB).",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Storage OK: {availableBytes / (1024 * 1024)} MB available.",
                data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Storage health check failed.", ex);
        }
    }
}
