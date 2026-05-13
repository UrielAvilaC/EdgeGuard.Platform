using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Diagnostics.HealthChecks;

/// <summary>
/// Health check that verifies the HL7 TCP listener is bound and accepting connections
/// on the configured port. Performs a lightweight socket connect/disconnect test.
/// </summary>
public sealed class Hl7ListenerHealthCheck : IHealthCheck
{
    private readonly int _port;
    private readonly TimeSpan _timeout;
    private readonly ILogger<Hl7ListenerHealthCheck> _logger;

    public Hl7ListenerHealthCheck(
        IOptions<Hl7ListenerHealthOptions> options,
        ILogger<Hl7ListenerHealthCheck> logger)
    {
        _port = options.Value.Port;
        _timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["Port"] = _port,
            ["TimeoutSeconds"] = _timeout.TotalSeconds
        };

        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);

            await client.ConnectAsync("127.0.0.1", _port, cts.Token);

            data["Connected"] = true;

            return HealthCheckResult.Healthy(
                $"HL7 TCP listener is accepting connections on port {_port}.", data);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("HL7 listener health check timed out on port {Port}", _port);
            data["Connected"] = false;
            data["Reason"] = "Timeout";

            return HealthCheckResult.Degraded(
                $"HL7 listener connection timed out on port {_port}.", data: data);
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "HL7 listener health check failed on port {Port}", _port);
            data["Connected"] = false;
            data["Reason"] = ex.SocketErrorCode.ToString();

            return HealthCheckResult.Unhealthy(
                $"HL7 listener unreachable on port {_port}: {ex.SocketErrorCode}.",
                ex, data);
        }
    }
}

/// <summary>
/// Configuration for the HL7 listener health check.
/// </summary>
public sealed class Hl7ListenerHealthOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Hl7Listener";

    /// <summary>HL7 TCP listener port to check.</summary>
    public int Port { get; set; } = 8001;

    /// <summary>Timeout in seconds for the connection test.</summary>
    public int TimeoutSeconds { get; set; } = 5;
}
