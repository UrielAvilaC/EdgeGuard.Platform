using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Models.Core;
using Dicom.Edge.Models.Enums;
using Dicom.Edge.Models.Patient;
using Dicom.Edge.Node.DicomServer;
using Dicom.Edge.Node.Persistence.Context;
using FellowOakDicom;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Node;

/// <summary>
/// Handles DICOM instances received by the C-STORE SCP.
/// Saves the file to local storage and upserts study/series/instance records in the database.
/// The <see cref="Dicom.Edge.Node.Persistence.Services.StudyCompletionWatcherService"/> will
/// later detect completed studies by polling <see cref="DicomStudy.LastImageReceivedAt"/>.
/// </summary>
/// <remarks>
/// Registered as <b>Singleton</b> because <see cref="DicomServerHostedService"/> (also singleton)
/// captures the handler reference. A new <see cref="IServiceScope"/> is created per
/// <see cref="HandleInstanceAsync"/> call so the scoped <see cref="EdgeNodeDbContext"/>
/// is resolved correctly and disposed after each C-STORE.
/// </remarks>
internal sealed class DicomInstanceHandler(
    IServiceScopeFactory scopeFactory,
    INodeSettingsService settings,
    ILogger<DicomInstanceHandler> logger) : IDicomInstanceHandler
{
    public async Task HandleInstanceAsync(
        DicomDataset dataset,
        string callingAeTitle,
        CancellationToken ct = default)
    {
        var studyUid = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);
        var seriesUid = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty);
        var sopUid = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);

        if (string.IsNullOrWhiteSpace(studyUid) || string.IsNullOrWhiteSpace(sopUid))
        {
            logger.LogWarning(
                "Received DICOM instance with missing UIDs (Study={StudyUid}, SOP={SopUid}) from {CallingAe} — skipping",
                studyUid, sopUid, callingAeTitle);
            return;
        }

        logger.LogInformation(
            "C-STORE received: Study={StudyUid}, Series={SeriesUid}, SOP={SopUid}, CallingAE={CallingAe}",
            studyUid, seriesUid, sopUid, callingAeTitle);

        // ── Save DICOM file to local storage ─────────────────────────────────
        var storageCfg = await settings.GetStorageConfigAsync(ct);
        var dicomFile = new DicomFile(dataset);

        var instanceDir = Path.Combine(storageCfg.RootPath, studyUid, seriesUid);
        Directory.CreateDirectory(instanceDir);

        var filePath = Path.Combine(instanceDir, sopUid + NodeConstants.DicomFileExtension);
        var tempPath = filePath + NodeConstants.TempFileExtension;

        await dicomFile.SaveAsync(tempPath);
        File.Move(tempPath, filePath, overwrite: true);

        var fileSize = new FileInfo(filePath).Length;

        logger.LogDebug("Stored instance {SopUid} at {Path} ({FileSize} bytes)", sopUid, filePath, fileSize);

        // ── Persist study / series / instance to DB ──────────────────────────
        var now = DateTime.UtcNow;
        var sopClassUid = dataset.GetSingleValueOrDefault(DicomTag.SOPClassUID, string.Empty);
        var modality = dataset.GetSingleValueOrDefault(DicomTag.Modality, NodeConstants.DefaultModality);
        var instanceNumber = dataset.GetSingleValueOrDefault(DicomTag.InstanceNumber, 0);

        await using var scope = scopeFactory.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<EdgeNodeDbContext>();

        // ── Upsert Patient ───────────────────────────────────────────────
        var patientId   = dataset.GetSingleValueOrDefault(DicomTag.PatientID,   NodeConstants.DefaultPatientId);
        var patientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);

        // Extract all available DICOM patient demographics
        var birthDate         = dataset.TryGetSingleValue(DicomTag.PatientBirthDate, out DateTime bd) ? bd : (DateTime?)null;
        var sex               = dataset.GetSingleValueOrDefault<string?>(DicomTag.PatientSex,            null);
        var patientAge        = dataset.GetSingleValueOrDefault<string?>(DicomTag.PatientAge,            null);
        var patientWeightKg   = dataset.TryGetSingleValue(DicomTag.PatientWeight, out double wt)         ? wt : (double?)null;
        var patientHeightM    = dataset.TryGetSingleValue(DicomTag.PatientSize,   out double ht)         ? ht : (double?)null;
        var accessionNumber   = dataset.GetSingleValueOrDefault<string?>(DicomTag.AccessionNumber,       null);
        var referringPhysician= dataset.GetSingleValueOrDefault<string?>(DicomTag.ReferringPhysicianName,null);
        var institutionName   = dataset.GetSingleValueOrDefault<string?>(DicomTag.InstitutionName,       null);

        var patient = await ctx.Patients.FirstOrDefaultAsync(p => p.PatientId == patientId, ct);

        if (patient is null)
        {
            patient = new DicomPatient
            {
                PatientId          = patientId,
                PatientName        = patientName,
                BirthDate          = birthDate,
                Sex                = sex ?? string.Empty,
                PatientAge         = patientAge,
                PatientWeightKg    = patientWeightKg,
                PatientHeightM     = patientHeightM,
                AccessionNumber    = accessionNumber,
                ReferringPhysician = referringPhysician,
                InstitutionName    = institutionName,
            };

            ctx.Patients.Add                      (patient);
            await ctx.SaveChangesAsync(ct);

            logger.LogDebug("Created new patient record PatientId={PatientId}", patientId);
        }
        else
        {
            // Update fields that may have changed or been absent in earlier instances
            if (!string.IsNullOrWhiteSpace(patientName))        patient.PatientName        = patientName;
            if (birthDate.HasValue)                             patient.BirthDate          = birthDate;
            if (!string.IsNullOrWhiteSpace(sex))               patient.Sex                = sex!;
            if (!string.IsNullOrWhiteSpace(patientAge))        patient.PatientAge         = patientAge;
            if (patientWeightKg.HasValue)                      patient.PatientWeightKg    = patientWeightKg;
            if (patientHeightM.HasValue)                       patient.PatientHeightM     = patientHeightM;
            if (!string.IsNullOrWhiteSpace(accessionNumber))   patient.AccessionNumber    = accessionNumber;
            if (!string.IsNullOrWhiteSpace(referringPhysician))patient.ReferringPhysician = referringPhysician;
            if (!string.IsNullOrWhiteSpace(institutionName))   patient.InstitutionName    = institutionName;
        }

        // ── Upsert Study ─────────────────────────────────────────────────
        var study = await ctx.Studies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.StudyInstanceUid == studyUid, ct);

        if (study is null)
        {
            var generalCfg = await settings.GetGeneralConfigAsync(ct);

            study = new DicomStudy
            {
                StudyInstanceUid = studyUid,
                PatientId = patientId,
                PatientName = patientName,
                StudyDate = dataset.GetSingleValueOrDefault(DicomTag.StudyDate, now),
                Status = StudyStatus.Receiving,
                InstanceCount = 1,
                LastImageReceivedAt = now,
                ReceivedAt = now,
                SourceAeTitle = callingAeTitle,
                EdgeNodeId = generalCfg.NodeName,
                TotalSizeBytes = fileSize,
                StudyDescription = dataset.GetSingleValueOrDefault<string?>(DicomTag.StudyDescription, null),
                AccessionNumber = dataset.GetSingleValueOrDefault<string?>(DicomTag.AccessionNumber, null),
                ReferringPhysician = dataset.GetSingleValueOrDefault<string?>(DicomTag.ReferringPhysicianName, null),
            };

            ctx.Studies.Add(study);
        }
        else
        {
            study.InstanceCount++;
            study.LastImageReceivedAt = now;
            study.TotalSizeBytes += fileSize;
        }

        // ── Upsert Series ────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(seriesUid))
        {
            var series = await ctx.Series
                .FirstOrDefaultAsync(s => s.SeriesInstanceUid == seriesUid, ct);

            if (series is null)
            {
                series = new DicomSeries
                {
                    SeriesInstanceUid = seriesUid,
                    StudyInstanceUid = studyUid,
                    Modality = modality,
                    InstanceCount = 1,
                };

                ctx.Series.Add(series);
            }
            else
            {
                series.InstanceCount++;
            }
        }

        // ── Insert Instance ──────────────────────────────────────────────
        var existing = await ctx.Instances
            .FirstOrDefaultAsync(i => i.SopInstanceUid == sopUid, ct);

        if (existing is null)
        {
            var instance = new DicomInstance
            {
                SopInstanceUid = sopUid,
                SeriesInstanceUid = seriesUid,
                SopClassUid = sopClassUid,
                InstanceNumber = instanceNumber,
                FilePath = filePath,
            };

            ctx.Instances.Add(instance);

            // Set shadow properties tracked by EF configuration
            ctx.Entry(instance).Property(NodeConstants.ShadowPropertyFileSizeBytes).CurrentValue = fileSize;
            ctx.Entry(instance).Property(NodeConstants.ShadowPropertyTransferSyntaxUid).CurrentValue =
                dataset.InternalTransferSyntax?.UID?.UID;
        }
        else
        {
            // Duplicate SOP — update file path (overwritten on disk)
            existing.FilePath = filePath;
            ctx.Entry(existing).Property(NodeConstants.ShadowPropertyFileSizeBytes).CurrentValue = fileSize;
        }

        await ctx.SaveChangesAsync(ct);

        logger.LogDebug(
            "Persisted instance {SopUid} for study {StudyUid} (InstanceCount={Count})",
            sopUid, studyUid, study.InstanceCount);
    }
}
