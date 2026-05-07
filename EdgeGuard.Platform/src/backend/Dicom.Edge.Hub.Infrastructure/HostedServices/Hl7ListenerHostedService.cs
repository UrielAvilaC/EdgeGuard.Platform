using System.Net.Sockets;
using Dicom.Edge.Hub.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.HostedServices;

/// <summary>
/// Hosted service que ejecuta el HL7 Listener como un servicio de fondo.
/// </summary>
public class Hl7ListenerHostedService : BackgroundService
{
    private readonly IHl7Listener _listener;
    private readonly ILogger<Hl7ListenerHostedService> _logger;

    public Hl7ListenerHostedService(
        IHl7Listener listener,
        ILogger<Hl7ListenerHostedService> logger)
    {
        _listener = listener;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HL7 Listener Hosted Service starting...");

        try
        {
            await _listener.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("HL7 Listener Hosted Service was cancelled");
        }
        catch (SocketException se) when (
            se.SocketErrorCode == SocketError.OperationAborted ||
            stoppingToken.IsCancellationRequested)
        {
            // Windows IIS app pool recycle: socket abort propagates as SocketException 995,
            // not OperationCanceledException. Treat as normal shutdown to prevent StopHost.
            _logger.LogInformation("HL7 Listener Hosted Service stopped (host shutdown)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in HL7 Listener Hosted Service");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HL7 Listener Hosted Service stopping...");
        
        await _listener.StopAsync(cancellationToken);
        
        await base.StopAsync(cancellationToken);
        
        _logger.LogInformation("HL7 Listener Hosted Service stopped");
    }
}
