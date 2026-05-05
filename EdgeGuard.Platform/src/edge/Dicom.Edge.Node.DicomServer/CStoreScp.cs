using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Dicom.Edge.Node.DicomServer;

/// <summary>
/// fo-dicom SCP provider — handles incoming DICOM associations with support for:
/// <list type="bullet">
///   <item><description><b>C-STORE</b> — image/object reception delegated to <see cref="IDicomInstanceHandler"/>.</description></item>
///   <item><description><b>C-FIND MWL</b> — Modality Worklist queries delegated to <see cref="IWorklistCFindHandler"/>.</description></item>
///   <item><description><b>C-ECHO</b> — Verification SCP; responds to connectivity pings from remote systems.</description></item>
/// </list>
/// Dependencies are passed via <see cref="DicomService.UserState"/> (<see cref="DicomScpDependencies"/>)
/// because fo-dicom creates a new service instance per association.
/// </summary>
public sealed class CStoreScp : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCFindProvider, IDicomCEchoProvider
{
    
    private DicomScpDependencies? _deps;
    private IAssociationSession? _session;

    /// <summary>Lazily resolves the dependencies record set as UserState by the server.</summary>
    private DicomScpDependencies Deps => _deps ??= (DicomScpDependencies)UserState;

    public CStoreScp(
        INetworkStream stream,
        Encoding fallbackEncoding,
        ILogger logger,
        DicomServiceDependencies dependencies)
        : base(stream, fallbackEncoding, logger, dependencies)
    {
        
    }


    // ── Association lifecycle ────────────────────────────────────────────────

    public async Task OnReceiveAssociationRequestAsync(DicomAssociation association)
    {
        var options = Deps.Options;

        // DICOM AE titles can have trailing spaces — always trim before comparing.
        var calledAe  = association.CalledAE.Trim();
        var callingAe = association.CallingAE.Trim();
        var localAe   = options.AeTitle.Trim();

        Deps.Logger.LogInformation(
            "Association request — CallingAE={CallingAe} CalledAE={CalledAe} LocalAE={LocalAe} Host={Host}",
            callingAe, calledAe, localAe, association.RemoteHost);

        if (!string.Equals(calledAe, localAe, StringComparison.OrdinalIgnoreCase))
        {
            Deps.Logger.LogWarning(
                "Association REJECTED — CalledAE '{CalledAe}' does not match local AE '{LocalAe}'. " +
                "The remote SCU must use the correct AE Title. " +
                "Check DicomServer:AeTitle in appsettings or the value pushed from the Hub (dicom.ae_title in DB).",
                calledAe, localAe);
            await Deps.AssociationTracker.RecordRejectionAsync(
                callingAe, calledAe,
                association.RemoteHost ?? string.Empty,
                association.RemotePort,
                $"CalledAE '{calledAe}' not recognized (local='{localAe}')");
            await SendAssociationRejectAsync(
                DicomRejectResult.Permanent,
                DicomRejectSource.ServiceUser,
                DicomRejectReason.CalledAENotRecognized);
            return;
        }

        // ── CallingAE whitelist validation ────────────────────────────────────
        if (options.ValidateCallingAe &&
            options.AllowedCallingAeTitles.Length > 0 &&
            !options.AllowedCallingAeTitles.Contains(callingAe, StringComparer.OrdinalIgnoreCase))
        {
            Deps.Logger.LogWarning(
                "Association REJECTED — CallingAE '{CallingAe}' is not in the allowed list ({Allowed})",
                callingAe,
                string.Join(", ", options.AllowedCallingAeTitles));
            await Deps.AssociationTracker.RecordRejectionAsync(
                callingAe, calledAe,
                association.RemoteHost ?? string.Empty,
                association.RemotePort,
                $"CallingAE '{callingAe}' not in allowed list");
            await SendAssociationRejectAsync(
                DicomRejectResult.Permanent,
                DicomRejectSource.ServiceUser,
                DicomRejectReason.CallingAENotRecognized);
            return;
        }

        // ── Presentation context negotiation ─────────────────────────────────
        var accepted = 0;
        var rejected = 0;
        var acceptedUids = new System.Text.StringBuilder();

        foreach (var ctx in association.PresentationContexts)
        {
            var uid = ctx.AbstractSyntax;

            // C-ECHO — Verification SOP Class
            if (uid == DicomUID.Verification)
            {
                if (options.CEchoEnabled)
                {
                    ctx.SetResult(DicomPresentationContextResult.Accept);
                    accepted++;
                    if (acceptedUids.Length > 0) acceptedUids.Append(", ");
                    acceptedUids.Append(uid.UID);
                    Deps.Logger.LogInformation(
                        "Presentation context ACCEPTED — C-ECHO (Verification) ID={Id} CallingAE={CallingAe}",
                        ctx.ID, callingAe);
                }
                else
                {
                    ctx.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                    rejected++;
                    Deps.Logger.LogWarning(
                        "Presentation context REJECTED — C-ECHO disabled (CEchoEnabled=false). " +
                        "Enable it via Hub setting dicom.cecho_enabled=true.");
                }
                continue;
            }

            // MWL C-FIND
            if (uid == DicomUID.ModalityWorklistInformationModelFind)
            {
                if (options.MwlEnabled)
                {
                    ctx.SetResult(DicomPresentationContextResult.Accept);
                        accepted++;
                        if (acceptedUids.Length > 0) acceptedUids.Append(", ");
                        acceptedUids.Append(uid.UID);
                        Deps.Logger.LogDebug("Presentation context ACCEPTED — MWL C-FIND ID={Id}", ctx.ID);
                }
                else
                {
                    ctx.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                    rejected++;
                    Deps.Logger.LogDebug("Presentation context REJECTED — MWL disabled (MwlEnabled=false)");
                }
                continue;
            }

            // C-STORE — accept all storage SOP classes
            ctx.SetResult(DicomPresentationContextResult.Accept);
            accepted++;
            if (acceptedUids.Length > 0) acceptedUids.Append(", ");
            acceptedUids.Append(uid.UID);
        }

        Deps.Logger.LogInformation(
            "Association negotiation complete — CallingAE={CallingAe} Accepted={Accepted} Rejected={Rejected}",
            callingAe, accepted, rejected);

        _session = await Deps.AssociationTracker.BeginAsync(
            callingAe, calledAe,
            association.RemoteHost ?? string.Empty,
            association.RemotePort,
            acceptedUids.Length > 0 ? acceptedUids.ToString() : null);

        await SendAssociationAcceptAsync(association);
    }

    public async Task OnReceiveAssociationReleaseRequestAsync()
    {
        Deps.Logger.LogDebug("Association released — requesting immediate completion check");
        Deps.CompletionTrigger.RequestImmediateCheck();
        if (_session is not null) await _session.CompleteAsync();
        await SendAssociationReleaseResponseAsync();
    }

    public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
    {
        Deps.Logger.LogWarning("Association aborted: source={Source}, reason={Reason}", source, reason);
        if (_session is not null)
            _ = _session.AbortAsync($"Aborted — source={source}, reason={reason}");
    }

    public void OnConnectionClosed(Exception? exception)
    {
        if (exception is not null)
        {
            Deps.Logger.LogWarning(exception, "DICOM connection closed with error");
            if (_session is not null)
                _ = _session.AbortAsync(exception.Message);
        }
    }

    // ── C-STORE (image reception) ───────────────────────────────────────────

    public async Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
    {
        try
        {
            await Deps.InstanceHandler.HandleInstanceAsync(
                request.Dataset,
                Association.CallingAE,
                CancellationToken.None);

            _session?.RecordImage();
            return new DicomCStoreResponse(request, DicomStatus.Success);
        }
        catch (Exception ex)
        {
            Deps.Logger.LogError(ex, "C-STORE processing error for SOP {SopInstanceUid}",
                request.SOPInstanceUID?.UID);
            return new DicomCStoreResponse(request, DicomStatus.ProcessingFailure);
        }
    }

    public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e)
    {
        Deps.Logger.LogError(e, "C-STORE exception for temp file {TempFile}", tempFileName);
        return Task.CompletedTask;
    }

    // ── C-FIND MWL (Modality Worklist query) ────────────────────────────────

    public async IAsyncEnumerable<DicomCFindResponse> OnCFindRequestAsync(
        DicomCFindRequest request)
    {
        // Only handle Modality Worklist queries
        if (request.SOPClassUID != DicomUID.ModalityWorklistInformationModelFind)
        {
            Deps.Logger.LogWarning("Unsupported C-FIND SOP class: {SopClassUid}", request.SOPClassUID);
            yield return new DicomCFindResponse(request, DicomStatus.SOPClassNotSupported);
            yield break;
        }

        Deps.Logger.LogInformation("MWL C-FIND request from {CallingAe}", Association.CallingAE);

        int resultCount = 0;

        await foreach (var dataset in Deps.MwlHandler.QueryWorklistAsync(request.Dataset))
        {
            var response = new DicomCFindResponse(request, DicomStatus.Pending)
            {
                Dataset = dataset
            };

            resultCount++;
            yield return response;
        }

        Deps.Logger.LogInformation(
            "MWL C-FIND completed — {Count} results for {CallingAe}",
            resultCount, Association.CallingAE);

        yield return new DicomCFindResponse(request, DicomStatus.Success);
    }

    // ── C-ECHO (Verification) ────────────────────────────────────────────────

    /// <summary>
    /// Responds to C-ECHO (DICOM ping) requests.
    /// Used by remote systems to verify connectivity and DICOM compatibility.
    /// </summary>
    public Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request)
    {
        Deps.Logger.LogInformation(
            "C-ECHO received from {CallingAe} — responding with Success",
            Association.CallingAE);

        return Task.FromResult(new DicomCEchoResponse(request, DicomStatus.Success));
    }

}
