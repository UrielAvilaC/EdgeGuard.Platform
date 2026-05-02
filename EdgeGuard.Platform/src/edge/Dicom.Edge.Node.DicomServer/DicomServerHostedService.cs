using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Node.DicomServer;

/// <summary>
/// Background service that starts and manages the fo-dicom SCP server.
/// Supports both C-STORE (image reception) and C-FIND MWL (worklist queries).
/// The listen port is fixed at startup (fo-dicom requires it). AeTitle and MwlEnabled
/// are read via <see cref="IOptionsMonitor{T}"/> so Hub config pushes take effect
/// without a service restart.
/// </summary>
public sealed class DicomServerHostedService(
    IDicomInstanceHandler instanceHandler,
    IWorklistCFindHandler mwlHandler,
    IOptionsMonitor<DicomServerOptions> optionsMonitor,
    ILogger<DicomServerHostedService> logger) : BackgroundService
{
    private FellowOakDicom.Network.IDicomServer? _server;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Snapshot options at startup — port cannot change while running
        var startupOpts = optionsMonitor.CurrentValue;

        if (!startupOpts.Enabled)
        {
            logger.LogInformation("DICOM server is disabled via configuration");
            return;
        }

        logger.LogInformation(
            "Starting DICOM SCP on port {Port}, AE Title={AeTitle}, MWL={MwlEnabled}",
            startupOpts.Port, startupOpts.AeTitle, startupOpts.MwlEnabled);

        _server = FellowOakDicom.Network.DicomServerFactory.Create<CStoreScp>(
            startupOpts.Port,
            userState: new DicomScpDependencies(instanceHandler, mwlHandler, optionsMonitor));

        logger.LogInformation(
            "DICOM server listening on port {Port} — C-STORE=enabled, MWL={MwlEnabled}",
            startupOpts.Port, startupOpts.MwlEnabled);

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
/// Uses <see cref="IOptionsMonitor{T}"/> so AeTitle and MwlEnabled changes pushed from the
/// Hub are reflected in subsequent associations without a service restart.
/// </summary>
public sealed record DicomScpDependencies(
    IDicomInstanceHandler InstanceHandler,
    IWorklistCFindHandler MwlHandler,
    IOptionsMonitor<DicomServerOptions> OptionsMonitor)
{
    public DicomServerOptions Options => OptionsMonitor.CurrentValue;
}
