using Dicom.Edge.Diagnostics.Correlation;
using Dicom.Edge.Diagnostics.Logging;
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
///   <item><description><b>C-FIND Study Root Q/R</b> — study queries delegated to <see cref="IStudyRootCFindHandler"/>.</description></item>
///   <item><description><b>C-ECHO</b> — Verification SCP; responds to connectivity pings from remote systems.</description></item>
/// </list>
/// Dependencies are passed via <see cref="DicomService.UserState"/> (<see cref="DicomScpDependencies"/>)
/// because fo-dicom creates a new service instance per association.
/// </summary>
/// <remarks>
/// <b>Per-association logging.</b> One instance exists per association, so the association id
/// created in the constructor identifies it exactly. Two mechanisms tag the log events:
/// the decorated <see cref="DicomService.Logger"/> (covers fo-dicom's own events, emitted from
/// the connection read loop) and <see cref="DicomAssociationContext"/>, re-entered at the start
/// of every callback (covers everything running underneath: instance handler, C-FIND handlers,
/// storage, EF Core). The association file is opened before AE validation — so rejections are
/// captured too — and closed on release, abort or connection close.
/// </remarks>
public sealed class CStoreScp : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCFindProvider, IDicomCEchoProvider
{

    private DicomScpDependencies? _deps;
    private IAssociationSession? _session;

    private readonly AssociationLogContext _logCtx = AssociationLogContext.New();
    private readonly AssociationSummary _summary = new();
    private int _logClosed;

    /// <summary>Lazily resolves the dependencies record set as UserState by the server.</summary>
    private DicomScpDependencies Deps => _deps ??= (DicomScpDependencies)UserState;

    public CStoreScp(
        INetworkStream stream,
        Encoding fallbackEncoding,
        ILogger logger,
        DicomServiceDependencies dependencies)
        : base(stream, fallbackEncoding, logger, dependencies)
    {
        // Every event this service logs — including fo-dicom's internal PDU/DIMSE traffic —
        // is attributed to this association.
        Logger = new AssociationScopedLogger(logger, _logCtx);
    }


    // ── Association lifecycle ────────────────────────────────────────────────

    public async Task OnReceiveAssociationRequestAsync(DicomAssociation association)
    {
        using var logScope = DicomAssociationContext.Enter(_logCtx);

        var options = Deps.Options;

        // DICOM AE titles can have trailing spaces — always trim before comparing.
        var calledAe  = association.CalledAE.Trim();
        var callingAe = association.CallingAE.Trim();
        var localAe   = options.AeTitle.Trim();

        _logCtx.CallingAe  = callingAe;
        _logCtx.CalledAe   = calledAe;
        _logCtx.RemoteHost = association.RemoteHost ?? AssociationLogContext.Unknown;
        _logCtx.RemotePort = association.RemotePort;

        // Opened before validation so rejected associations get their own file too — that is
        // precisely the case support needs during modality homologation.
        Deps.AssociationLog.Open(_logCtx);

        Logger.LogInformation(
            "Association request — CallingAE={CallingAe} CalledAE={CalledAe} LocalAE={LocalAe} Host={Host} AssociationId={AssociationId}",
            callingAe, calledAe, localAe, association.RemoteHost, _logCtx.AssociationId);

        if (!options.IsAcceptedCalledAe(calledAe))
        {
            var reason = $"CalledAE '{calledAe}' not recognized (local='{localAe}')";

            Logger.LogWarning(
                "Association REJECTED — CalledAE '{CalledAe}' does not match local AE '{LocalAe}' or any alias ({Aliases}). " +
                "Update DicomServer:AeTitleAliases to accept this title, set ValidateCalledAe=false for permissive mode, " +
                "or correct the AE title on the remote SCU.",
                calledAe, localAe,
                options.AeTitleAliases.Length > 0 ? string.Join(", ", options.AeTitleAliases) : "(none)");
            await Deps.AssociationTracker.RecordRejectionAsync(
                callingAe, calledAe,
                association.RemoteHost ?? string.Empty,
                association.RemotePort,
                reason);
            await SendAssociationRejectAsync(
                DicomRejectResult.Permanent,
                DicomRejectSource.ServiceUser,
                DicomRejectReason.CalledAENotRecognized);
            CloseAssociationLog(AssociationOutcome.Rejected, reason);
            return;
        }

        // ── CallingAE validation ──────────────────────────────────────────────
        // Preferred: the equipment catalog is the authority. When it is populated, only
        // equipment that is registered AND enabled may associate (strict mode).
        // Fallback: when the catalog is empty (e.g. a fresh node before the first Hub
        // push), fall back to the legacy AllowedCallingAeTitles whitelist so the node is
        // not bricked during rollout.
        if (!Deps.EquipmentCatalog.IsEmpty)
        {
            var equipment = Deps.EquipmentCatalog.FindByAeTitle(callingAe);
            if (equipment is null || !equipment.IsEnabled)
            {
                var reason = equipment is null
                    ? $"CallingAE '{callingAe}' is not registered in the equipment catalog"
                    : $"CallingAE '{callingAe}' is registered but disabled";

                Logger.LogWarning(
                    "Association REJECTED — {Reason}. Register/enable the equipment on the Hub " +
                    "(node {CalledAe}) to allow this modality.",
                    reason, calledAe);
                await Deps.AssociationTracker.RecordRejectionAsync(
                    callingAe, calledAe,
                    association.RemoteHost ?? string.Empty,
                    association.RemotePort,
                    reason);
                await SendAssociationRejectAsync(
                    DicomRejectResult.Permanent,
                    DicomRejectSource.ServiceUser,
                    DicomRejectReason.CallingAENotRecognized);
                CloseAssociationLog(AssociationOutcome.Rejected, reason);
                return;
            }

            // Enterprise AE+IP enforcement: when the equipment declares an IP address,
            // the remote host must match it. The accept/reject decision is persisted by
            // the association tracker (dicom_associations) for the equipment audit trail.
            if (!string.IsNullOrWhiteSpace(equipment.IpAddress))
            {
                var remoteHost = association.RemoteHost?.Trim() ?? string.Empty;
                if (!string.Equals(remoteHost, equipment.IpAddress.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    var reason =
                        $"CallingAE '{callingAe}' source IP mismatch (expected '{equipment.IpAddress}', got '{remoteHost}')";

                    Logger.LogWarning(
                        "Association REJECTED — {Reason}. Update the equipment IP on the Hub or correct the modality network configuration.",
                        reason);
                    await Deps.AssociationTracker.RecordRejectionAsync(
                        callingAe, calledAe,
                        association.RemoteHost ?? string.Empty,
                        association.RemotePort,
                        reason);
                    await SendAssociationRejectAsync(
                        DicomRejectResult.Permanent,
                        DicomRejectSource.ServiceUser,
                        DicomRejectReason.CallingAENotRecognized);
                    CloseAssociationLog(AssociationOutcome.Rejected, reason);
                    return;
                }
            }

            // Passive presence: the equipment is catalogued, enabled and (if required) IP-matched.
            // Record the association time so the Hub can show last-seen / online status.
            Deps.EquipmentActivityTracker.RecordSeen(callingAe);
        }
        else if (options.ValidateCallingAe &&
                 options.AllowedCallingAeTitles.Length > 0 &&
                 !options.AllowedCallingAeTitles.Contains(callingAe, StringComparer.OrdinalIgnoreCase))
        {
            var reason = $"CallingAE '{callingAe}' not in allowed list";

            Logger.LogWarning(
                "Association REJECTED — CallingAE '{CallingAe}' is not in the allowed list ({Allowed})",
                callingAe,
                string.Join(", ", options.AllowedCallingAeTitles));
            await Deps.AssociationTracker.RecordRejectionAsync(
                callingAe, calledAe,
                association.RemoteHost ?? string.Empty,
                association.RemotePort,
                reason);
            await SendAssociationRejectAsync(
                DicomRejectResult.Permanent,
                DicomRejectSource.ServiceUser,
                DicomRejectReason.CallingAENotRecognized);
            CloseAssociationLog(AssociationOutcome.Rejected, reason);
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
                    Logger.LogInformation(
                        "Presentation context ACCEPTED — C-ECHO (Verification) ID={Id} CallingAE={CallingAe}",
                        ctx.ID, callingAe);
                }
                else
                {
                    ctx.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                    rejected++;
                    Logger.LogWarning(
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
                        Logger.LogDebug("Presentation context ACCEPTED — MWL C-FIND ID={Id}", ctx.ID);
                }
                else
                {
                    ctx.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                    rejected++;
                    Logger.LogDebug("Presentation context REJECTED — MWL disabled (MwlEnabled=false)");
                }
                continue;
            }

            // Study Root Query/Retrieve C-FIND
            if (uid == DicomUID.StudyRootQueryRetrieveInformationModelFind)
            {
                if (options.QrEnabled)
                {
                    ctx.SetResult(DicomPresentationContextResult.Accept);
                    accepted++;
                    if (acceptedUids.Length > 0) acceptedUids.Append(", ");
                    acceptedUids.Append(uid.UID);
                    Logger.LogDebug("Presentation context ACCEPTED — Study Root C-FIND ID={Id}", ctx.ID);
                }
                else
                {
                    ctx.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                    rejected++;
                    Logger.LogDebug(
                        "Presentation context REJECTED — Query/Retrieve disabled (QrEnabled=false). " +
                        "Enable it via node settings key EnableDicomQr or dicom.qr_enabled.");
                }
                continue;
            }

            // C-STORE — accept all storage SOP classes
            ctx.SetResult(DicomPresentationContextResult.Accept);
            accepted++;
            if (acceptedUids.Length > 0) acceptedUids.Append(", ");
            acceptedUids.Append(uid.UID);
        }

        Logger.LogInformation(
            "Association negotiation complete — CallingAE={CallingAe} Accepted={Accepted} Rejected={Rejected}",
            callingAe, accepted, rejected);

        _summary.Outcome = AssociationOutcome.Accepted;
        _summary.AcceptedContexts = acceptedUids.Length > 0 ? acceptedUids.ToString() : null;

        _session = await Deps.AssociationTracker.BeginAsync(
            callingAe, calledAe,
            association.RemoteHost ?? string.Empty,
            association.RemotePort,
            _summary.AcceptedContexts);

        await SendAssociationAcceptAsync(association);
    }

    public async Task OnReceiveAssociationReleaseRequestAsync()
    {
        using var logScope = DicomAssociationContext.Enter(_logCtx);

        Logger.LogDebug("Association released — requesting immediate completion check");
        Deps.CompletionTrigger.RequestImmediateCheck();
        if (_session is not null) await _session.CompleteAsync();
        await SendAssociationReleaseResponseAsync();

        CloseAssociationLog(AssociationOutcome.Completed, null);
    }

    public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
    {
        Logger.LogWarning("Association aborted: source={Source}, reason={Reason}", source, reason);
        if (_session is not null)
            _ = _session.AbortAsync($"Aborted — source={source}, reason={reason}");

        CloseAssociationLog(AssociationOutcome.Aborted, $"source={source}, reason={reason}");
    }

    public void OnConnectionClosed(Exception? exception)
    {
        if (exception is not null)
        {
            Logger.LogWarning(exception, "DICOM connection closed with error");
            _summary.RecordError();
            if (_session is not null)
                _ = _session.AbortAsync(exception.Message);
        }

        // No-op when the association was already released, aborted or rejected.
        CloseAssociationLog(
            exception is null ? AssociationOutcome.Closed : AssociationOutcome.Aborted,
            exception?.Message);
    }

    /// <summary>
    /// Writes the summary footer and releases the per-association file. Idempotent: the first
    /// caller wins, so the abort/close pair that fo-dicom raises for a broken connection does
    /// not produce two footers.
    /// </summary>
    private void CloseAssociationLog(AssociationOutcome outcome, string? reason)
    {
        if (Interlocked.Exchange(ref _logClosed, 1) != 0) return;

        _summary.Outcome = outcome;
        _summary.Reason  = reason;

        // A logging failure must never break an association.
        try
        {
            _deps?.AssociationLog.Close(_logCtx, _summary);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to close per-association log {AssociationId}", _logCtx.AssociationId);
        }
    }

    // ── C-STORE (image reception) ───────────────────────────────────────────

    public async Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
    {
        using var logScope = DicomAssociationContext.Enter(_logCtx);

        try
        {
            await Deps.InstanceHandler.HandleInstanceAsync(
                request.Dataset,
                Association.CallingAE,
                CancellationToken.None);

            _session?.RecordImage();
            _summary.RecordCStore(success: true);
            return new DicomCStoreResponse(request, DicomStatus.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "C-STORE processing error for SOP {SopInstanceUid}",
                request.SOPInstanceUID?.UID);
            _summary.RecordCStore(success: false);
            _summary.RecordError();
            return new DicomCStoreResponse(request, DicomStatus.ProcessingFailure);
        }
    }

    public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e)
    {
        using var logScope = DicomAssociationContext.Enter(_logCtx);

        Logger.LogError(e, "C-STORE exception for temp file {TempFile}", tempFileName);
        _summary.RecordError();
        return Task.CompletedTask;
    }

    // ── C-FIND handlers (MWL + Study Root Q/R) ──────────────────────────────

    public async IAsyncEnumerable<DicomCFindResponse> OnCFindRequestAsync(
        DicomCFindRequest request)
    {
        using var logScope = DicomAssociationContext.Enter(_logCtx);

        // MWL (Modality Worklist) queries
        if (request.SOPClassUID == DicomUID.ModalityWorklistInformationModelFind)
        {
            Logger.LogInformation("MWL C-FIND request from {CallingAe}", Association.CallingAE);

            int resultCount = 0;

            await foreach (var dataset in Deps.MwlHandler.QueryWorklistAsync(
                               request.Dataset, Association.CallingAE.Trim()))
            {
                var response = new DicomCFindResponse(request, DicomStatus.Pending)
                {
                    Dataset = dataset
                };

                resultCount++;
                yield return response;
            }

            Logger.LogInformation(
                "MWL C-FIND completed — {Count} results for {CallingAe}",
                resultCount, Association.CallingAE);
            _summary.RecordMwlQuery(resultCount);

            yield return new DicomCFindResponse(request, DicomStatus.Success);
            yield break;
        }

        // Study Root Query/Retrieve queries
        if (request.SOPClassUID == DicomUID.StudyRootQueryRetrieveInformationModelFind)
        {
            if (!Deps.Options.QrEnabled)
            {
                Logger.LogWarning("Study Root C-FIND requested while Q/R is disabled");
                yield return new DicomCFindResponse(request, DicomStatus.SOPClassNotSupported);
                yield break;
            }

            var queryKeys = request.Dataset ?? new DicomDataset();
            var level = queryKeys.GetSingleValueOrDefault(DicomTag.QueryRetrieveLevel, "STUDY");

            if (!string.Equals(level, "STUDY", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning(
                    "Unsupported Q/R level for Study Root C-FIND: {Level}",
                    level);
                yield return new DicomCFindResponse(request, DicomStatus.QueryRetrieveUnableToProcess);
                yield break;
            }

            Logger.LogInformation("Study Root C-FIND request from {CallingAe}", Association.CallingAE);
            var resultCount = 0;

            await foreach (var dataset in Deps.StudyRootHandler.QueryStudiesAsync(queryKeys))
            {
                var response = new DicomCFindResponse(request, DicomStatus.Pending)
                {
                    Dataset = dataset
                };
                resultCount++;
                yield return response;
            }

            Logger.LogInformation(
                "Study Root C-FIND completed — {Count} results for {CallingAe}",
                resultCount, Association.CallingAE);
            _summary.RecordQrQuery(resultCount);

            yield return new DicomCFindResponse(request, DicomStatus.Success);
            yield break;
        }

        Logger.LogWarning("Unsupported C-FIND SOP class: {SopClassUid}", request.SOPClassUID);
        yield return new DicomCFindResponse(request, DicomStatus.SOPClassNotSupported);
    }

    // ── C-ECHO (Verification) ────────────────────────────────────────────────

    /// <summary>
    /// Responds to C-ECHO (DICOM ping) requests.
    /// Used by remote systems to verify connectivity and DICOM compatibility.
    /// </summary>
    public Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request)
    {
        using var logScope = DicomAssociationContext.Enter(_logCtx);

        Logger.LogInformation(
            "C-ECHO received from {CallingAe} — responding with Success",
            Association.CallingAE);
        _summary.RecordCEcho();

        return Task.FromResult(new DicomCEchoResponse(request, DicomStatus.Success));
    }

}
