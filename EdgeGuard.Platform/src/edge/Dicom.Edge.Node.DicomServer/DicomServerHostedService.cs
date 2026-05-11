using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Node.DicomServer;

/// <summary>
/// Background service that starts and manages the fo-dicom SCP server.
/// Supports both C-STORE (image reception) and C-FIND MWL (worklist queries).
/// </summary>
public sealed class DicomServerHostedService(
    IDicomInstanceHandler instanceHandler,
    IWorklistCFindHandler mwlHandler,
    IOptions<DicomServerOptions> options,
    ILogger<DicomServerHostedService> logger) : BackgroundService
{
    private readonly DicomServerOptions _opts = options.Value;
    private FellowOakDicom.Network.IDicomServer? _server;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opts.Enabled)
        {
            logger.LogInformation("DICOM server is disabled via configuration");
            return;
        }

        logger.LogInformation(
            "Starting DICOM SCP on port {Port}, AE Title={AeTitle}, MWL={MwlEnabled}",
            _opts.Port, _opts.AeTitle, _opts.MwlEnabled);

        _server = FellowOakDicom.Network.DicomServerFactory.Create<CStoreScp>(
            _opts.Port,
            userState: new DicomScpDependencies(instanceHandler, mwlHandler, _opts));

        logger.LogInformation(
            "DICOM server listening on port {Port} — C-STORE=enabled, MWL={MwlEnabled}",
            _opts.Port, _opts.MwlEnabled);

        // Keep alive until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_server is not null)
        {
            _server.Dispose();
            logger.LogInformation("DICOM server stopped");
        }
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Passes all custom dependencies to <see cref="CStoreScp"/> via fo-dicom's user state mechanism.
/// The SCP resolves these lazily from <see cref="FellowOakDicom.Network.DicomService.UserState"/>.
/// </summary>
public sealed record DicomScpDependencies(
    IDicomInstanceHandler InstanceHandler,
    IWorklistCFindHandler MwlHandler,
    DicomServerOptions Options);
