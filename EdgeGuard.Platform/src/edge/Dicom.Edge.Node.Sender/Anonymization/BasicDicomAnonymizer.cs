using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using FellowOakDicom;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Sender.Anonymization;

/// <summary>
/// P0-2: Implements the DICOM PS3.15 Annex E Basic Confidentiality Profile.
/// Removes / redacts PHI tags and remaps UIDs via a deterministic SHA-256 hash so
/// referential integrity between Study/Series/SOP Instance UIDs is preserved
/// across all instances of the same study sent to the same PACS.
/// </summary>
public sealed class BasicDicomAnonymizer(ILogger<BasicDicomAnonymizer> logger) : IDicomAnonymizer
{
    // ── PHI tags to remove entirely ──────────────────────────────────────────
    private static readonly DicomTag[] TagsToRemove =
    [
        DicomTag.PatientAddress,
        DicomTag.PatientTelephoneNumbers,
        DicomTag.OtherPatientIDsRETIRED,
        DicomTag.OtherPatientNames,
        DicomTag.ReferringPhysicianName,
        DicomTag.PhysiciansOfRecord,
        DicomTag.PerformingPhysicianName,
        DicomTag.NameOfPhysiciansReadingStudy,
        DicomTag.OperatorsName,
        DicomTag.InstitutionAddress,
        DicomTag.InstitutionalDepartmentName,
        DicomTag.RequestingPhysician,
        DicomTag.RequestingService,
        DicomTag.PatientMotherBirthName,
        DicomTag.PatientBirthName,
        DicomTag.MilitaryRank,
        DicomTag.MedicalRecordLocatorRETIRED,
    ];

    // ── PHI tags to replace with deterministic hash ──────────────────────────
    private static readonly DicomTag[] TagsToHash =
    [
        DicomTag.PatientID,
        DicomTag.AccessionNumber,
        DicomTag.StudyID,
        DicomTag.InstitutionName,
    ];

    // ── UIDs to remap deterministically (preserve referential integrity) ─────
    private static readonly DicomTag[] UidTagsToRemap =
    [
        DicomTag.StudyInstanceUID,
        DicomTag.SeriesInstanceUID,
        DicomTag.SOPInstanceUID,
        DicomTag.FrameOfReferenceUID,
        DicomTag.MediaStorageSOPInstanceUID,
    ];

    /// <summary>OID arc for UUID-derived UIDs (per DICOM PS3.5).</summary>
    private const string AnonymizedRootUid = "2.25.";

    /// <summary>Literal that replaces patient names.</summary>
    private const string AnonymizedName = "ANONYMIZED";

    public DicomFile Anonymize(DicomFile source, AnonymizationProfile profile)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        var dataset = source.Dataset.Clone();

        // 1. PatientName → literal
        if (dataset.Contains(DicomTag.PatientName))
            dataset.AddOrUpdate(DicomTag.PatientName, AnonymizedName);

        // 2. PatientBirthDate → year only (yyyy0101) to preserve age cohort
        if (dataset.Contains(DicomTag.PatientBirthDate))
        {
            var raw = dataset.GetSingleValueOrDefault(DicomTag.PatientBirthDate, string.Empty);
            if (raw.Length >= 4)
                dataset.AddOrUpdate(DicomTag.PatientBirthDate, raw[..4] + "0101");
        }

        // 3. Remove PHI tags entirely
        foreach (var tag in TagsToRemove)
            dataset.Remove(tag);

        // 4. Hash identified tags (deterministic — same input → same output)
        foreach (var tag in TagsToHash)
        {
            if (!dataset.Contains(tag)) continue;
            var original = dataset.GetSingleValueOrDefault(tag, string.Empty);
            if (!string.IsNullOrEmpty(original))
                dataset.AddOrUpdate(tag, HashString(original, length: 16));
        }

        // 5. Remap UIDs (deterministic — same UID → same anonymized UID)
        foreach (var tag in UidTagsToRemap)
        {
            if (!dataset.Contains(tag)) continue;
            var original = dataset.GetSingleValueOrDefault(tag, string.Empty);
            if (!string.IsNullOrEmpty(original))
                dataset.AddOrUpdate(tag, RemapUid(original));
        }

        // 6. De-identification audit (DICOM PS3.15 Section E.1)
        dataset.AddOrUpdate(DicomTag.PatientIdentityRemoved, "YES");
        dataset.AddOrUpdate(DicomTag.DeidentificationMethod, "EdgeGuard Basic Confidentiality (DICOM PS3.15 Annex E)");

        var anonymized = new DicomFile(dataset);

        // Mirror the SOP Instance UID remap in the File Meta Information
        if (anonymized.FileMetaInfo.Contains(DicomTag.MediaStorageSOPInstanceUID))
        {
            var orig = anonymized.FileMetaInfo.GetSingleValueOrDefault(
                DicomTag.MediaStorageSOPInstanceUID, string.Empty);
            if (!string.IsNullOrEmpty(orig))
                anonymized.FileMetaInfo.AddOrUpdate(DicomTag.MediaStorageSOPInstanceUID, RemapUid(orig));
        }

        logger.LogDebug("Anonymized DICOM dataset (profile {Profile})", profile);
        return anonymized;
    }

    /// <summary>SHA-256 hash, hex-encoded, truncated to <paramref name="length"/> chars.</summary>
    private static string HashString(string input, int length)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var hex = Convert.ToHexString(bytes);
        return hex.Length > length ? hex[..length] : hex;
    }

    /// <summary>
    /// Maps an arbitrary UID to a deterministic anonymized UID under the
    /// "2.25." arc. The same input always produces the same output, so all
    /// instances of a study get consistent Study/Series UIDs after anonymization.
    /// </summary>
    private static string RemapUid(string original)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(original));
        // 12 bytes → 96-bit unsigned BigInteger → ≤30 decimal digits; total ≤35 chars (<<64 cap).
        var bigInt = new BigInteger(bytes.AsSpan(0, 12), isUnsigned: true, isBigEndian: true);
        return AnonymizedRootUid + bigInt.ToString();
    }
}
