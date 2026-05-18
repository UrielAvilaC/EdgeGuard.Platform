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

        try
        {
            var client = DicomClientFactory.Create(
                destination.Host, destination.Port,
                destination.UseTls, Opts.LocalAeTitle, destination.AeTitle);

            client.ClientOptions.AssociationRequestTimeoutInMs = Opts.TimeoutSeconds * 1000;

            var sent = 0;
            var failed = 0;

            if (destination.AnonymizeBeforeSend)
                logger.LogInformation(
                    "Anonymization enabled for {AeTitle} — applying Basic Confidentiality profile",
                    destination.AeTitle);

            foreach (var filePath in dicomFiles)
            {
                try
                {
                    var file = await DicomFile.OpenAsync(filePath);

                    // P0-2: Strip PHI before transmitting when the destination requires it.
                    if (destination.AnonymizeBeforeSend)
                        file = anonymizer.Anonymize(file, AnonymizationProfile.BasicConfidentiality);

                    var request = new DicomCStoreRequest(file);
                    request.OnResponseReceived += (_, response) =>
                    {
                        if (response.Status == DicomStatus.Success)
                            Interlocked.Increment(ref sent);
                        else
                            Interlocked.Increment(ref failed);
                    };
                    await client.AddRequestAsync(request);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to read DICOM file {Path}", filePath);
                    Interlocked.Increment(ref failed);
                }
            }

            await client.SendAsync(ct);
            sw.Stop();

            logger.LogInformation(
                "Study {StudyUid} sent to {AeTitle}: {Sent} ok, {Failed} failed in {Duration:N1}s",
                studyInstanceUid, destination.AeTitle, sent, failed, sw.Elapsed.TotalSeconds);

            return PacsSendResult.Ok(studyInstanceUid, destination.AeTitle, sent, sw.Elapsed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send study {StudyUid} to {AeTitle}",
                studyInstanceUid, destination.AeTitle);
            return PacsSendResult.Fail(studyInstanceUid, destination.AeTitle, ex.Message);
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
