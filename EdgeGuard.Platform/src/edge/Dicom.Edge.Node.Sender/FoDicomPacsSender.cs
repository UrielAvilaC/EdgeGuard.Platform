using System.Diagnostics;
using Dicom.Edge.Abstractions.Storage;
using Dicom.Edge.Node.Sender.Anonymization;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Node.Sender;

/// <summary>
/// fo-dicom C-STORE SCU implementation. Reads DICOM files from local storage
/// and sends them to the target PACS via C-STORE association.
/// Uses <see cref="IOptionsMonitor{T}"/> so that sender settings pushed from the Hub
/// (timeouts, retries) are picked up without restarting the service.
/// </summary>
public sealed class FoDicomPacsSender(
    IStorageProvider storageProvider,
    IDicomAnonymizer anonymizer,
    IOptionsMonitor<PacsSenderOptions> optionsMonitor,
    ILogger<FoDicomPacsSender> logger) : IPacsSender
{
    private PacsSenderOptions Opts => optionsMonitor.CurrentValue;

    public async Task<PacsSendResult> SendStudyAsync(
        string studyInstanceUid,
        PacsDestination destination,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        logger.LogInformation(
            "Sending study {StudyUid} to {AeTitle}@{Host}:{Port}",
            studyInstanceUid, destination.AeTitle, destination.Host, destination.Port);

        var studyPathResult = await storageProvider.GetStudyPathAsync(studyInstanceUid, ct);
        if (!studyPathResult.IsSuccess || string.IsNullOrEmpty(studyPathResult.Value))
            return PacsSendResult.Fail(studyInstanceUid, destination.AeTitle, "Study path not found");

        var studyPath = studyPathResult.Value;
        if (!Directory.Exists(studyPath))
            return PacsSendResult.Fail(studyInstanceUid, destination.AeTitle, "Study directory does not exist");

        var dicomFiles = Directory.GetFiles(studyPath, "*.dcm", SearchOption.AllDirectories);
        if (dicomFiles.Length == 0)
            return PacsSendResult.Fail(studyInstanceUid, destination.AeTitle, "No DICOM files found");

        if (destination.AnonymizeBeforeSend)
            logger.LogInformation(
                "Anonymization enabled for {AeTitle} — applying Basic Confidentiality profile",
                destination.AeTitle);

        // P1: Retry on transient failures. Each attempt re-sends ONLY the instances
        // that were not acknowledged as success. C-STORE is idempotent by SOP Instance
        // UID, so re-sending an already-stored object is safe.
        var pending = new List<string>(dicomFiles);
        string? lastError = null;
        var maxAttempts = Math.Max(1, Opts.MaxRetries);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            (pending, lastError) = await SendBatchAsync(pending, destination, ct);

            if (pending.Count == 0)
                break;

            if (attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt)));
                logger.LogWarning(
                    "Study {StudyUid} → {AeTitle}: {Failed} instance(s) failed on attempt {Attempt}/{Max}, retrying in {Delay:N0}s",
                    studyInstanceUid, destination.AeTitle, pending.Count, attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }

        sw.Stop();
        var succeeded = dicomFiles.Length - pending.Count;

        // P1 (2a): A partially-failed transfer must NOT be reported as success —
        // otherwise instances are silently lost while the study is marked "sent".
        if (pending.Count > 0)
        {
            logger.LogError(
                "Study {StudyUid} to {AeTitle}: {Sent}/{Total} sent, {Failed} failed after {Attempts} attempt(s)",
                studyInstanceUid, destination.AeTitle, succeeded, dicomFiles.Length, pending.Count, maxAttempts);

            return PacsSendResult.Fail(studyInstanceUid, destination.AeTitle,
                $"{pending.Count}/{dicomFiles.Length} instance(s) failed after {maxAttempts} attempt(s). {lastError}".Trim());
        }

        logger.LogInformation(
            "Study {StudyUid} sent to {AeTitle}: {Sent} ok in {Duration:N1}s",
            studyInstanceUid, destination.AeTitle, succeeded, sw.Elapsed.TotalSeconds);

        return PacsSendResult.Ok(studyInstanceUid, destination.AeTitle, succeeded, sw.Elapsed);
    }

    /// <summary>
    /// Sends one batch of DICOM files over a single association and returns the subset
    /// that was NOT acknowledged as success (to be retried) together with the last error.
    /// An association-level failure marks the whole batch as failed so it is retried.
    /// </summary>
    private async Task<(List<string> Failed, string? Error)> SendBatchAsync(
        IReadOnlyList<string> files,
        PacsDestination destination,
        CancellationToken ct)
    {
        var failed = new System.Collections.Concurrent.ConcurrentBag<string>();

        try
        {
            var client = DicomClientFactory.Create(
                destination.Host, destination.Port,
                destination.UseTls, Opts.LocalAeTitle, destination.AeTitle);

            client.ClientOptions.AssociationRequestTimeoutInMs = Opts.TimeoutSeconds * 1000;

            foreach (var filePath in files)
            {
                try
                {
                    var file = await DicomFile.OpenAsync(filePath);

                    // P0-2: Strip PHI before transmitting when the destination requires it.
                    if (destination.AnonymizeBeforeSend)
                        file = anonymizer.Anonymize(file, AnonymizationProfile.BasicConfidentiality);

                    var capturedPath = filePath;
                    var request = new DicomCStoreRequest(file);
                    request.OnResponseReceived += (_, response) =>
                    {
                        // Only failures are tracked; anything not added here succeeded.
                        if (response.Status != DicomStatus.Success)
                            failed.Add(capturedPath);
                    };
                    await client.AddRequestAsync(request);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to read DICOM file {Path}", filePath);
                    failed.Add(filePath);
                }
            }

            await client.SendAsync(ct);
            return (failed.Distinct().ToList(), null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Association-level failure: no instance in this batch can be considered
            // delivered, so the whole batch is retried (idempotent by SOP Instance UID).
            logger.LogWarning(ex, "C-STORE association to {AeTitle} failed", destination.AeTitle);
            return (files.ToList(), ex.Message);
        }
    }

    public async Task<PacsCEchoVerifyResult> VerifyConnectionAsync(PacsDestination destination, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var client = DicomClientFactory.Create(
                destination.Host, destination.Port,
                destination.UseTls, Opts.LocalAeTitle, destination.AeTitle);

            client.ClientOptions.AssociationRequestTimeoutInMs = Opts.TimeoutSeconds * 1000;

            var echoRequest = new DicomCEchoRequest();
            var dicomSuccess = false;
            string? dicomStatusDescription = null;
            echoRequest.OnResponseReceived += (_, response) =>
            {
                dicomSuccess = response.Status == DicomStatus.Success;
                if (!dicomSuccess)
                    dicomStatusDescription = response.Status?.Description ?? response.Status?.ToString();
            };

            await client.AddRequestAsync(echoRequest);
            await client.SendAsync(ct);
            sw.Stop();

            return dicomSuccess
                ? PacsCEchoVerifyResult.Ok(sw.Elapsed.TotalMilliseconds)
                : PacsCEchoVerifyResult.Fail(
                    $"C-ECHO returned non-success status: {dicomStatusDescription}",
                    dicomStatusDescription);
        }
        catch (DicomAssociationRejectedException ex)
        {
            sw.Stop();
            // Extract the structured rejection reason from the DICOM exception
            var reason = ex.RejectReason.ToString();
            logger.LogWarning(ex, "C-ECHO to {AeTitle}@{Host}:{Port} failed — AssociationRejected Reason={Reason}",
                destination.AeTitle, destination.Host, destination.Port, reason);
            return PacsCEchoVerifyResult.Fail(ex.Message, reason);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "C-ECHO to {AeTitle}@{Host}:{Port} failed",
                destination.AeTitle, destination.Host, destination.Port);
            return PacsCEchoVerifyResult.Fail(ex.Message);
        }
    }
}
