using System.Diagnostics;
using Dicom.Edge.Abstractions.Storage;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Node.Sender;

/// <summary>
/// fo-dicom C-STORE SCU implementation. Reads DICOM files from local storage
/// and sends them to the target PACS via C-STORE association.
/// </summary>
public sealed class FoDicomPacsSender(
    IStorageProvider storageProvider,
    IOptions<PacsSenderOptions> options,
    ILogger<FoDicomPacsSender> logger) : IPacsSender
{
    private readonly PacsSenderOptions _opts = options.Value;

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
                destination.UseTls, _opts.LocalAeTitle, destination.AeTitle);

            client.ClientOptions.AssociationRequestTimeoutInMs = _opts.TimeoutSeconds * 1000;

            var sent = 0;
            var failed = 0;

            foreach (var filePath in dicomFiles)
            {
                try
                {
                    var file = await DicomFile.OpenAsync(filePath);
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

    public async Task<bool> VerifyConnectionAsync(PacsDestination destination, CancellationToken ct = default)
    {
        try
        {
            var client = DicomClientFactory.Create(
                destination.Host, destination.Port,
                destination.UseTls, _opts.LocalAeTitle, destination.AeTitle);

            client.ClientOptions.AssociationRequestTimeoutInMs = _opts.TimeoutSeconds * 1000;

            var echoRequest = new DicomCEchoRequest();
            var success = false;
            echoRequest.OnResponseReceived += (_, response) =>
            {
                success = response.Status == DicomStatus.Success;
            };

            await client.AddRequestAsync(echoRequest);
            await client.SendAsync(ct);

            return success;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "C-ECHO to {AeTitle}@{Host}:{Port} failed",
                destination.AeTitle, destination.Host, destination.Port);
            return false;
        }
    }
}
