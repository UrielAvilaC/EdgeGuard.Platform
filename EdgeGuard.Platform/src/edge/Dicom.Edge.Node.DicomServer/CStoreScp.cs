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
/// </list>
/// Dependencies are passed via <see cref="DicomService.UserState"/> (<see cref="DicomScpDependencies"/>)
/// because fo-dicom creates a new service instance per association.
/// </summary>
public sealed class CStoreScp : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCFindProvider
{
    private readonly ILogger<CStoreScp> _logger;
    private DicomScpDependencies? _deps;

    /// <summary>Lazily resolves the dependencies record set as UserState by the server.</summary>
    private DicomScpDependencies Deps => _deps ??= (DicomScpDependencies)UserState;

    public CStoreScp(
        INetworkStream stream,
        Encoding fallbackEncoding,
        ILogger<CStoreScp> logger,
        DicomServiceDependencies dependencies)
        : base(stream, fallbackEncoding, logger, dependencies)
    {
        _logger = logger;
    }

    // ── Association lifecycle ────────────────────────────────────────────────

    public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
    {
        var options = Deps.Options;

        _logger.LogInformation(
            "Association request from {CallingAe} -> {CalledAe}",
            association.CallingAE, association.CalledAE);

        if (!string.Equals(association.CalledAE, options.AeTitle, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Rejected: CalledAE {CalledAe} does not match {Expected}",
                association.CalledAE, options.AeTitle);
            return SendAssociationRejectAsync(
                DicomRejectResult.Permanent,
                DicomRejectSource.ServiceUser,
                DicomRejectReason.CalledAENotRecognized);
        }

        //if (options.AllowedCallingAeTitles.Length > 0 &&
        //    !options.AllowedCallingAeTitles.Contains(association.CallingAE, StringComparer.OrdinalIgnoreCase))
        //{
        //    _logger.LogWarning("Rejected: CallingAE {CallingAe} not in allowed list",
        //        association.CallingAE);
        //    return SendAssociationRejectAsync(
        //        DicomRejectResult.Permanent,
        //        DicomRejectSource.ServiceUser,
        //        DicomRejectReason.CallingAENotRecognized);
        //}

        foreach (var ctx in association.PresentationContexts)
        {
            // Accept C-STORE and MWL C-FIND presentation contexts
            if (ctx.AbstractSyntax == DicomUID.ModalityWorklistInformationModelFind &&
                !options.MwlEnabled)
            {
                _logger.LogDebug("MWL C-FIND rejected — MWL is disabled in configuration");
                ctx.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                continue;
            }

            ctx.SetResult(DicomPresentationContextResult.Accept);
        }

        return SendAssociationAcceptAsync(association);
    }

    public Task OnReceiveAssociationReleaseRequestAsync()
    {
        _logger.LogDebug("Association released");
        return SendAssociationReleaseResponseAsync();
    }

    public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason) =>
        _logger.LogWarning("Association aborted: source={Source}, reason={Reason}", source, reason);

    public void OnConnectionClosed(Exception? exception)
    {
        if (exception is not null)
            _logger.LogWarning(exception, "DICOM connection closed with error");
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

            return new DicomCStoreResponse(request, DicomStatus.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "C-STORE processing error for SOP {SopInstanceUid}",
                request.SOPInstanceUID?.UID);
            return new DicomCStoreResponse(request, DicomStatus.ProcessingFailure);
        }
    }

    public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e)
    {
        _logger.LogError(e, "C-STORE exception for temp file {TempFile}", tempFileName);
        return Task.CompletedTask;
    }

    // ── C-FIND MWL (Modality Worklist query) ────────────────────────────────

    public async IAsyncEnumerable<DicomCFindResponse> OnCFindRequestAsync(
        DicomCFindRequest request)
    {
        // Only handle Modality Worklist queries
        if (request.SOPClassUID != DicomUID.ModalityWorklistInformationModelFind)
        {
            _logger.LogWarning("Unsupported C-FIND SOP class: {SopClassUid}", request.SOPClassUID);
            yield return new DicomCFindResponse(request, DicomStatus.SOPClassNotSupported);
            yield break;
        }

        _logger.LogInformation("MWL C-FIND request from {CallingAe}", Association.CallingAE);

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

        _logger.LogInformation(
            "MWL C-FIND completed — {Count} results for {CallingAe}",
            resultCount, Association.CallingAE);

        yield return new DicomCFindResponse(request, DicomStatus.Success);
    }
}
