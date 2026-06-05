namespace Dicom.Edge.Models.Enums
{
    /// <summary>
    /// Format of the diagnostic report attached to a study (from ORU OBX segments).
    /// </summary>
    public enum ReportFormat
    {
        /// <summary>No textual/HTML report attached (a PDF may still be present).</summary>
        None,

        /// <summary>Report body is HTML markup.</summary>
        Html,

        /// <summary>Report body is plain text.</summary>
        PlainText
    }
}
