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
        using var activity = StorageActivitySource.StartSaveInstance(sopUid);
        var targetPath = StoragePaths.InstanceFile(rootPath, studyUid, seriesUid, sopUid);
        var directory = Path.GetDirectoryName(targetPath)!;

        var dirCreated = !Directory.Exists(directory);
        Directory.CreateDirectory(directory);
        if (dirCreated)
            logger.LogDebug("Created directory {Dir} for study {StudyUid}/series {SeriesUid}",
                directory, studyUid, seriesUid);

        // Write to temp file first, then atomic rename to prevent partial files
        var tempPath = targetPath + ".tmp";
        var sw = System.Diagnostics.Stopwatch.StartNew();
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
            sw.Stop();

            activity?.SetTag("storage.bytes", data.Length);
            activity?.SetTag("storage.duration_ms", sw.ElapsedMilliseconds);
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
            logger.LogDebug(
                "Stored DICOM instance {SopUid} ({Bytes} bytes, {Ms}ms) → {Path}",
                sopUid, data.Length, sw.ElapsedMilliseconds, targetPath);

            return Path.GetFullPath(targetPath);
        }
        catch (Exception ex)
        {
            sw.Stop();
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex,
                "Failed to write DICOM instance {SopUid} ({Bytes} bytes, {Ms}ms) → {Path}",
                sopUid, data.Length, sw.ElapsedMilliseconds, targetPath);

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
        using var activity = StorageActivitySource.StartSaveInstance(sopUid);
        var targetPath = StoragePaths.InstanceFile(rootPath, studyUid, seriesUid, sopUid);
        var directory = Path.GetDirectoryName(targetPath)!;

        var dirCreated = !Directory.Exists(directory);
        Directory.CreateDirectory(directory);
        if (dirCreated)
            logger.LogDebug("Created directory {Dir} for study {StudyUid}/series {SeriesUid}",
                directory, studyUid, seriesUid);

        var tempPath = targetPath + ".tmp";
        var sw = System.Diagnostics.Stopwatch.StartNew();
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
            sw.Stop();

            var fileInfo = new FileInfo(targetPath);
            activity?.SetTag("storage.bytes", fileInfo.Length);
            activity?.SetTag("storage.duration_ms", sw.ElapsedMilliseconds);
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
            logger.LogDebug(
                "Stored DICOM instance {SopUid} ({Bytes} bytes, {Ms}ms) from stream → {Path}",
                sopUid, fileInfo.Length, sw.ElapsedMilliseconds, targetPath);

            return Path.GetFullPath(targetPath);
        }
        catch (Exception ex)
        {
            sw.Stop();
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex,
                "Failed to write DICOM instance {SopUid} from stream ({Ms}ms) → {Path}",
                sopUid, sw.ElapsedMilliseconds, targetPath);

            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); }
                catch { /* best effort */ }
            }
            throw;
        }
    }
}
