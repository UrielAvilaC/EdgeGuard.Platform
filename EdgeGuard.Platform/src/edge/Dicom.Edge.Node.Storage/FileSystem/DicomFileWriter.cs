using Dicom.Edge.Node.Storage.Constants;
using Dicom.Edge.Node.Storage.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Storage.FileSystem;

/// <summary>
/// Handles atomic writes of DICOM file bytes to disk.
/// Uses write-to-temp + rename to prevent partial files on crash.
/// </summary>
internal sealed class DicomFileWriter(ILogger<DicomFileWriter> logger)
{
    /// <summary>
    /// Writes DICOM bytes to the canonical path, creating directories as needed.
    /// Returns the final absolute path of the stored file.
    /// </summary>
    /// <param name="rootPath">Storage root directory.</param>
    /// <param name="studyUid">Study Instance UID.</param>
    /// <param name="seriesUid">Series Instance UID.</param>
    /// <param name="sopUid">SOP Instance UID.</param>
    /// <param name="data">Raw DICOM file bytes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The absolute file path where the instance was stored.</returns>
    public async Task<string> WriteAsync(
        string rootPath,
        string studyUid,
        string seriesUid,
        string sopUid,
        ReadOnlyMemory<byte> data,
        CancellationToken ct = default)
    {
        var targetPath = StoragePaths.InstanceFile(rootPath, studyUid, seriesUid, sopUid);
        var directory = Path.GetDirectoryName(targetPath)!;

        Directory.CreateDirectory(directory);

        // Write to temp file first, then atomic rename to prevent partial files
        var tempPath = targetPath + ".tmp";
        try
        {
            await using (var fs = new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await fs.WriteAsync(data, ct);
                await fs.FlushAsync(ct);
            }

            // Atomic rename — if target exists (duplicate), overwrite
            File.Move(tempPath, targetPath, overwrite: true);

            logger.LogDebug(
                "Stored DICOM instance {SopUid} ({Bytes} bytes) → {Path}",
                sopUid, data.Length, targetPath);

            return Path.GetFullPath(targetPath);
        }
        catch
        {
            // Cleanup temp file on failure
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); }
                catch { /* best effort */ }
            }
            throw;
        }
    }

    /// <summary>
    /// Writes DICOM bytes from a source stream (for large files that shouldn't be buffered).
    /// </summary>
    public async Task<string> WriteFromStreamAsync(
        string rootPath,
        string studyUid,
        string seriesUid,
        string sopUid,
        Stream source,
        CancellationToken ct = default)
    {
        var targetPath = StoragePaths.InstanceFile(rootPath, studyUid, seriesUid, sopUid);
        var directory = Path.GetDirectoryName(targetPath)!;

        Directory.CreateDirectory(directory);

        var tempPath = targetPath + ".tmp";
        try
        {
            await using (var fs = new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await source.CopyToAsync(fs, ct);
                await fs.FlushAsync(ct);
            }

            File.Move(tempPath, targetPath, overwrite: true);

            var fileInfo = new FileInfo(targetPath);
            logger.LogDebug(
                "Stored DICOM instance {SopUid} ({Bytes} bytes) from stream → {Path}",
                sopUid, fileInfo.Length, targetPath);

            return Path.GetFullPath(targetPath);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); }
                catch { /* best effort */ }
            }
            throw;
        }
    }
}
