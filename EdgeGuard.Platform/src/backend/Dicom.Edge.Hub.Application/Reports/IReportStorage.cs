namespace Dicom.Edge.Hub.Application.Reports;

/// <summary>
/// Persists diagnostic report PDFs physically in the Hub workspace and reads them back.
/// Interface lives in the Application layer (DIP); implementation in Infrastructure.
/// </summary>
public interface IReportStorage
{
    /// <summary>
    /// Stores the report PDF for a study and returns the relative path (within the
    /// workspace) to be saved on the study and used later to stream the file.
    /// </summary>
    Task<string> SavePdfAsync(string studyId, byte[] pdf, CancellationToken ct = default);

    /// <summary>Opens the stored PDF for reading, or null when it does not exist.</summary>
    Task<Stream?> OpenPdfAsync(string relativePath, CancellationToken ct = default);
}
