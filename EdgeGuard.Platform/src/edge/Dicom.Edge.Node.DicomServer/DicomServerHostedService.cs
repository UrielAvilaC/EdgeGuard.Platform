using Dicom.Edge.Abstractions.Events;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Node.DicomServer;

/// <summary>
/// Hosted service that starts and manages the fo-dicom SCP server.
/// Supports both C-STORE (image reception) and C-FIND MWL (worklist queries).
/// The listen port is fixed at startup (fo-dicom requires it). AeTitle and MwlEnabled
/// are read via <see cref="IOptionsMonitor{T}"/> so Hub config pushes take effect
/// without a service restart.
/// </summary>
/// <remarks>
/// Implements <see cref="IHostedService"/> directly instead of inheriting
/// <see cref="BackgroundService"/> because fo-dicom manages its own threads internally —
/// there is no worker loop. <see cref="StartAsync"/> returns immediately after the server
/// is created; <see cref="StopAsync"/> disposes it.
/// </remarks>
public sealed class DicomServerHostedService(
    IDicomInstanceHandler instanceHandler,
    IWorklistCFindHandler mwlHandler,
    IStudyCompletionTrigger completionTrigger,
    IOptionsMonitor<DicomServerOptions> optionsMonitor,
    ILogger<DicomServerHostedService> logger) : IHostedService, IDisposable
{
    private FellowOakDicom.Network.IDicomServer? _server;
    private bool _disposed;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var opts = optionsMonitor.CurrentValue;

        if (!opts.Enabled)
        {
            logger.LogInformation("DICOM server is disabled via configuration");
            return Task.CompletedTask;
        }

        logger.LogInformation(
            "Starting DICOM SCP on port {Port} AeTitle={AeTitle} MWL={MwlEnabled} CEcho={CEchoEnabled} ValidateCallingAe={ValidateCallingAe}",
            opts.Port, opts.AeTitle, opts.MwlEnabled, opts.CEchoEnabled, opts.ValidateCallingAe);

        new DicomSetupBuilder().RegisterServices(s => s.AddFellowOakDicom()).Build();

        _server = DicomServerFactory.Create<CStoreScp>(ipAddress: "*",
            opts.Port,
            userState: new DicomScpDependencies(instanceHandler, mwlHandler, completionTrigger, optionsMonitor, logger));

        logger.LogInformation(
            "DICOM server listening on port {Port} — C-STORE=enabled C-ECHO={CEchoEnabled} MWL={MwlEnabled}",
            opts.Port, opts.CEchoEnabled, opts.MwlEnabled);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _server?.Stop();
        logger.LogInformation("DICOM server stopped");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _server?.Dispose();
        _server = null;
        _disposed = true;
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
    IStudyCompletionTrigger CompletionTrigger,
    IOptionsMonitor<DicomServerOptions> OptionsMonitor,
    ILogger<DicomServerHostedService> Logger)
{
    public DicomServerOptions Options => OptionsMonitor.CurrentValue;
}
