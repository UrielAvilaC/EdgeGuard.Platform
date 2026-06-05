using System.Security.Cryptography.X509Certificates;
using Dicom.Edge.Abstractions.Events;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Tls;
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
    IStudyRootCFindHandler studyRootHandler,
    IStudyCompletionTrigger completionTrigger,
    IDicomAssociationTracker associationTracker,
    IOptionsMonitor<DicomServerOptions> optionsMonitor,
    IDicomServerFactory dicomServerFactory,
    ILogger<DicomServerHostedService> logger) : IHostedService, IDisposable
{
    private IDicomServer? _server;
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
            "Starting DICOM SCP on port {Port} AeTitle={AeTitle} MaxClients={MaxClients} MWL={MwlEnabled} QR={QrEnabled} CEcho={CEchoEnabled} ValidateCallingAe={ValidateCallingAe}",
            opts.Port, opts.AeTitle, opts.MaxClients, opts.MwlEnabled, opts.QrEnabled, opts.CEchoEnabled, opts.ValidateCallingAe);

        // AE unification: this is the single source of truth for the node AE. The SCU
        // Calling AE and the Hub-registration AE are derived from it. Validate format
        // (DICOM AE: 1-16 chars, A-Z 0-9 _) and warn — do not crash the node.
        if (!System.Text.RegularExpressions.Regex.IsMatch(opts.AeTitle ?? string.Empty, "^[A-Z0-9_]{1,16}$"))
            logger.LogWarning(
                "Node AE Title '{AeTitle}' is not a valid DICOM AE (expected 1-16 chars of A-Z, 0-9, _). " +
                "PACS or modalities may reject the association.",
                opts.AeTitle);

        // P0-3: When TLS is enabled, wrap the SCP listener with a TLS acceptor so
        // modality → Edge Node DICOM traffic (PHI) is encrypted in transit.
        ITlsAcceptor? tlsAcceptor = null;
        if (opts.Tls.Enabled)
        {
            if (string.IsNullOrWhiteSpace(opts.Tls.CertificatePath))
            {
                logger.LogError(
                    "DICOM TLS is enabled but no CertificatePath configured — falling back to plaintext");
            }
            else
            {
                try
                {
                    var cert = new X509Certificate2(opts.Tls.CertificatePath, opts.Tls.CertificatePassword);
                    tlsAcceptor = new DefaultTlsAcceptor(cert)
                    {
                        RequireMutualAuthentication = opts.Tls.RequireClientCertificate,
                    };
                    logger.LogInformation(
                        "DICOM TLS enabled (certificate {Path}, mutualAuth={Mutual})",
                        opts.Tls.CertificatePath, opts.Tls.RequireClientCertificate);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to load DICOM TLS certificate from {Path} — falling back to plaintext",
                        opts.Tls.CertificatePath);
                }
            }
        }

        _server = dicomServerFactory.Create<CStoreScp>(
            port:     opts.Port,
            tlsAcceptor: tlsAcceptor,
            userState: new DicomScpDependencies(
                instanceHandler,
                mwlHandler,
                studyRootHandler,
                completionTrigger,
                associationTracker,
                optionsMonitor,
                logger),
            // P1-2: cap concurrent SCP associations so a flood of connections cannot
            // exhaust threads/memory (DoS). 0 = unlimited (previous behaviour).
            configure: serverOptions => serverOptions.MaxClientsAllowed = opts.MaxClients);

        // P1-2: propagate PDU size and DIMSE request timeout to the SCP service so
        // oversized PDUs and stalled associations are bounded. Set before any client
        // connects (StartAsync runs at startup).
        _server.Options.MaxPDULength   = (uint)opts.MaxPduLength;
        _server.Options.RequestTimeout = TimeSpan.FromSeconds(opts.DimseTimeoutSeconds);

        logger.LogInformation(
            "DICOM server listening on port {Port} — TLS={Tls} C-STORE=enabled C-ECHO={CEchoEnabled} MWL={MwlEnabled} QR={QrEnabled}",
            opts.Port, tlsAcceptor is not null, opts.CEchoEnabled, opts.MwlEnabled, opts.QrEnabled);

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
    IStudyRootCFindHandler StudyRootHandler,
    IStudyCompletionTrigger CompletionTrigger,
    IDicomAssociationTracker AssociationTracker,
    IOptionsMonitor<DicomServerOptions> OptionsMonitor,
    ILogger<DicomServerHostedService> Logger)
{
    public DicomServerOptions Options => OptionsMonitor.CurrentValue;
}
