using FellowOakDicom;

namespace Dicom.Edge.Node.Sender.Anonymization;

/// <summary>
/// P0-2: Applies an anonymization profile to a DICOM file before transmission to PACS.
/// Implementations must produce a NEW <see cref="DicomFile"/> (the original is left
/// untouched so the local archive retains the unredacted copy).
/// </summary>
public interface IDicomAnonymizer
{
    DicomFile Anonymize(DicomFile source, AnonymizationProfile profile);
}

/// <summary>
/// Supported anonymization profiles. Currently only the DICOM PS3.15 Annex E
/// Basic Confidentiality Profile is implemented.
/// </summary>
public enum AnonymizationProfile
{
    /// <summary>DICOM PS3.15 Annex E Basic Confidentiality Profile.</summary>
    BasicConfidentiality = 0,
}
