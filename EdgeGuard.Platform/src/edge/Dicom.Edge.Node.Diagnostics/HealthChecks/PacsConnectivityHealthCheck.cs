using Dicom.Edge.Node.Diagnostics.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Sockets;

namespace Dicom.Edge.Node.Diagnostics.HealthChecks;

/// <summary>
/// Checks TCP connectivity to a configured PACS destination.
/// Performs a lightweight socket connection test without DICOM handshake.
/// </summary>
public sealed class PacsConnectivityHealthCheck : IHealthCheck
{
    private readonly string _host;
    private readonly int _port;
    private readonly TimeSpan _timeout;
    private readonly ILogger<PacsConnectivityHealthCheck> _logger;

    public PacsConnectivityHealthCheck(
        IOptions<PacsConnectivityOptions> pacsOptions,
        IOptions<HealthCheckThresholdOptions> healthOptions,
        ILogger<PacsConnectivityHealthCheck> logger)
    {
        _host = pacsOptions.Value.Host;
        _port = pacsOptions.Value.Port;
        _timeout = TimeSpan.FromSeconds(healthOptions.Value.PacsTimeoutSeconds);
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_host))
        {
            return HealthCheckResult.Degraded(
                "PACS host is not configured.");
        }

        var data = new Dictionary<string, object>
        {
            ["Host"] = _host,
            ["Port"] = _port,
            ["TimeoutSeconds"] = _timeout.TotalSeconds
        };

        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);

            await client.ConnectAsync(_host, _port, cts.Token);

            data["Connected"] = true;

            return HealthCheckResult.Healthy(
                $"PACS reachable at {_host}:{_port}.", data);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("PACS connectivity check timed out for {Host}:{Port}", _host, _port);

            data["Connected"] = false;
            data["Reason"] = "Timeout";

            return HealthCheckResult.Degraded(
                $"PACS connection timed out at {_host}:{_port}.", data: data);
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "PACS connectivity check failed for {Host}:{Port}", _host, _port);

            data["Connected"] = false;
            data["Reason"] = ex.SocketErrorCode.ToString();

            return HealthCheckResult.Unhealthy(
                $"PACS unreachable at {_host}:{_port}: {ex.SocketErrorCode}.",
                ex, data);
        }
    }
}

/// <summary>
/// Configuration for the PACS connectivity health check target.
/// </summary>
public sealed class PacsConnectivityOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PacsDestination";

    /// <summary>PACS host address.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>PACS port number.</summary>
    public int Port { get; set; } = 104;

    /// <summary>PACS AE Title.</summary>
    public string AeTitle { get; set; } = string.Empty;
}
