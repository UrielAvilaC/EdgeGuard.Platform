namespace Dicom.Edge.Node.Storage.Constants;

/// <summary>
/// Conventions for DICOM file storage path layout.
/// Layout: <c>{RootPath}/{StudyUID}/{SeriesUID}/{SopUID}.dcm</c>
/// This three-level hierarchy keeps directory sizes manageable at scale.
/// </summary>
internal static class StoragePaths
{
    public const string DicomExtension = ".dcm";

    /// <summary>Builds the study directory path.</summary>
    public static string StudyDirectory(string rootPath, string studyInstanceUid)
        => Path.Combine(rootPath, SanitizeUid(studyInstanceUid));

    /// <summary>Builds the series directory path within a study.</summary>
    public static string SeriesDirectory(string rootPath, string studyInstanceUid, string seriesInstanceUid)
        => Path.Combine(rootPath, SanitizeUid(studyInstanceUid), SanitizeUid(seriesInstanceUid));

    /// <summary>Builds the full file path for a DICOM instance.</summary>
    public static string InstanceFile(
        string rootPath,
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid)
        => Path.Combine(
            rootPath,
            SanitizeUid(studyInstanceUid),
            SanitizeUid(seriesInstanceUid),
            SanitizeUid(sopInstanceUid) + DicomExtension);

    /// <summary>
    /// Sanitizes a DICOM UID for use as a directory/file name.
    /// UIDs contain only digits and dots — safe on all filesystems,
    /// but we guard against unexpected characters defensively.
    /// </summary>
    private static string SanitizeUid(string uid)
        => string.Create(uid.Length, uid, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                span[i] = char.IsLetterOrDigit(c) || c == '.' ? c : '_';
            }
        });
}
