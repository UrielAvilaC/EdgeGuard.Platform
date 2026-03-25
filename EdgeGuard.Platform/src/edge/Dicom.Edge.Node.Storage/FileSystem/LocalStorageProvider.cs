using System.Diagnostics;
using Dicom.Edge.Abstractions.Metrics;
using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Abstractions.Storage;
using Dicom.Edge.Common.Errors;
using Dicom.Edge.Common.Results;
using Dicom.Edge.Node.Storage.Constants;
using Dicom.Edge.Node.Storage.Diagnostics;
using Dicom.Edge.Node.Storage.Integrity;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Storage.FileSystem;

/// <summary>
/// Local filesystem implementation of <see cref="IStorageProvider"/>.
/// Uses <see cref="INodeSettingsService"/> to resolve <c>StorageConfig.RootPath</c>
/// on every call so Hub-pushed path changes take effect without restart.
/// <para>
/// <strong>Path layout:</strong>
/// <c>{RootPath}/{StudyUID}/{SeriesUID}/{SopUID}.dcm</c>
/// </para>
/// <para>
/// <strong>Thread-safety:</strong> All methods are stateless filesystem operations.
/// Directory creation is idempotent. File writes use temp+rename for atomicity.
/// </para>
/// </summary>
public sealed class LocalStorageProvider(
    INodeSettingsService settings,
    IMetricsCollector metrics,
    ILogger<LocalStorageProvider> logger) : IStorageProvider
{
    // ── Path Resolution ───────────────────────────────────────────────────────

    public async Task<Result<string>> GetStudyPathAsync(
        string studyInstanceUid, CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = await settings.GetStorageConfigAsync(cancellationToken);
            var path = StoragePaths.StudyDirectory(cfg.RootPath, studyInstanceUid);
            return Result<string>.Success(Path.GetFullPath(path));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resolve study path for {StudyUid}", studyInstanceUid);
            return Result<string>.Failure(new Error("STORAGE_PATH_FAILED", ex.Message));
        }
    }

    public async Task<Result<string>> GetInstancePathAsync(
        string sopInstanceUid, CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = await settings.GetStorageConfigAsync(cancellationToken);

            // Search for the .dcm file recursively — SOP UID is unique across the entire DB
            var root = new DirectoryInfo(cfg.RootPath);
            if (!root.Exists)
                return Result<string>.Failure(new Error("STORAGE_ROOT_NOT_FOUND",
                    $"Storage root '{cfg.RootPath}' does not exist"));

            var fileName = StoragePaths.InstanceFile("", "", "", sopInstanceUid);
            var baseName = Path.GetFileName(fileName);

            var match = root.EnumerateFiles(baseName, SearchOption.AllDirectories)
                .FirstOrDefault();

            return match is not null
                ? Result<string>.Success(match.FullName)
                : Result<string>.Failure(new Error("INSTANCE_NOT_FOUND",
                    $"Instance {sopInstanceUid} not found in storage"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resolve instance path for {SopUid}", sopInstanceUid);
            return Result<string>.Failure(new Error("STORAGE_PATH_FAILED", ex.Message));
        }
    }

    // ── Storage Management ────────────────────────────────────────────────────

    public async Task<Result<long>> GetAvailableSpaceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var activity = StorageActivitySource.StartSpaceCheck();
            var cfg = await settings.GetStorageConfigAsync(cancellationToken);

            Directory.CreateDirectory(cfg.RootPath);
            var driveInfo = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(cfg.RootPath))!);

            var available = driveInfo.AvailableFreeSpace;
            metrics.RecordStorageUsage(available / (1024 * 1024), driveInfo.TotalSize / (1024 * 1024));
            activity?.SetTag("storage.available.bytes", available);

            return Result<long>.Success(available);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get available storage space");
            return Result<long>.Failure(new Error("STORAGE_SPACE_FAILED", ex.Message));
        }
    }

    public async Task<Result<long>> GetTotalSpaceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = await settings.GetStorageConfigAsync(cancellationToken);
            Directory.CreateDirectory(cfg.RootPath);
            var driveInfo = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(cfg.RootPath))!);
            return Result<long>.Success(driveInfo.TotalSize);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get total storage space");
            return Result<long>.Failure(new Error("STORAGE_SPACE_FAILED", ex.Message));
        }
    }

    public async Task<Result<long>> GetStudySizeAsync(
        string studyInstanceUid, CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = await settings.GetStorageConfigAsync(cancellationToken);
            var studyDir = new DirectoryInfo(StoragePaths.StudyDirectory(cfg.RootPath, studyInstanceUid));

            if (!studyDir.Exists)
                return Result<long>.Success(0L);

            var totalBytes = studyDir
                .EnumerateFiles("*" + StoragePaths.DicomExtension, SearchOption.AllDirectories)
                .Sum(f => f.Length);

            return Result<long>.Success(totalBytes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to calculate study size for {StudyUid}", studyInstanceUid);
            return Result<long>.Failure(new Error("STORAGE_SIZE_FAILED", ex.Message));
        }
    }

    public async Task<Result<IEnumerable<string>>> ListStudiesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = await settings.GetStorageConfigAsync(cancellationToken);
            var root = new DirectoryInfo(cfg.RootPath);

            if (!root.Exists)
                return Result<IEnumerable<string>>.Success([]);

            // Each top-level directory is a study UID
            var studies = root
                .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
                .Select(d => d.Name)
                .ToList();

            return Result<IEnumerable<string>>.Success(studies);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list studies in storage");
            return Result<IEnumerable<string>>.Failure(new Error("STORAGE_LIST_FAILED", ex.Message));
        }
    }

    // ── File Operations ───────────────────────────────────────────────────────

    public async Task<Result> DeleteStudyAsync(
        string studyInstanceUid, CancellationToken cancellationToken = default)
    {
        using var activity = StorageActivitySource.StartDeleteStudy(studyInstanceUid);
        try
        {
            var cfg = await settings.GetStorageConfigAsync(cancellationToken);
            var studyDir = StoragePaths.StudyDirectory(cfg.RootPath, studyInstanceUid);

            if (!Directory.Exists(studyDir))
            {
                logger.LogDebug("Study directory not found for {StudyUid} — already cleaned", studyInstanceUid);
                return Result.Success();
            }

            var fileCount = Directory.EnumerateFiles(studyDir, "*", SearchOption.AllDirectories).Count();
            Directory.Delete(studyDir, recursive: true);

            activity?.SetTag("storage.deleted.files", fileCount);
            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation("Deleted study {StudyUid} from storage ({Files} files)", studyInstanceUid, fileCount);

            return Result.Success();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Failed to delete study {StudyUid} from storage", studyInstanceUid);
            return Result.Failure(new Error("STORAGE_DELETE_FAILED", ex.Message));
        }
    }

    public async Task<Result> DeleteInstanceAsync(
        string sopInstanceUid, CancellationToken cancellationToken = default)
    {
        try
        {
            var pathResult = await GetInstancePathAsync(sopInstanceUid, cancellationToken);
            if (pathResult.IsFailure)
                return Result.Failure(pathResult.Error!);

            File.Delete(pathResult.Value);
            logger.LogDebug("Deleted instance {SopUid} from storage", sopInstanceUid);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete instance {SopUid}", sopInstanceUid);
            return Result.Failure(new Error("STORAGE_DELETE_FAILED", ex.Message));
        }
    }

    public async Task<Result> ArchiveStudyAsync(
        string studyInstanceUid, string archivePath, CancellationToken cancellationToken = default)
    {
        using var activity = StorageActivitySource.StartArchiveStudy(studyInstanceUid);
        try
        {
            var cfg = await settings.GetStorageConfigAsync(cancellationToken);
            var sourceDir = StoragePaths.StudyDirectory(cfg.RootPath, studyInstanceUid);

            if (!Directory.Exists(sourceDir))
                return Result.Failure(new Error("STUDY_NOT_FOUND",
                    $"Study directory not found: {studyInstanceUid}"));

            var destDir = Path.Combine(archivePath, studyInstanceUid);
            Directory.CreateDirectory(destDir);

            // Copy all files preserving directory structure
            long totalBytes = 0;
            var fileCount = 0;
            foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourceDir, file);
                var destFile = Path.Combine(destDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                File.Copy(file, destFile, overwrite: true);
                totalBytes += new FileInfo(file).Length;
                fileCount++;
            }

            activity?.SetTag("storage.archive.files", fileCount);
            activity?.SetTag("storage.archive.bytes", totalBytes);
            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation(
                "Archived study {StudyUid}: {Files} files, {MB:F1} MB → {Dest}",
                studyInstanceUid, fileCount, totalBytes / (1024.0 * 1024.0), destDir);

            return Result.Success();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Failed to archive study {StudyUid}", studyInstanceUid);
            return Result.Failure(new Error("STORAGE_ARCHIVE_FAILED", ex.Message));
        }
    }

    // ── Integrity & Validation ────────────────────────────────────────────────

    public async Task<Result> VerifyIntegrityAsync(
        string studyInstanceUid, CancellationToken cancellationToken = default)
    {
        using var activity = StorageActivitySource.StartVerifyIntegrity(studyInstanceUid);
        try
        {
            var cfg = await settings.GetStorageConfigAsync(cancellationToken);
            var studyDir = StoragePaths.StudyDirectory(cfg.RootPath, studyInstanceUid);

            if (!Directory.Exists(studyDir))
                return Result.Failure(new Error("STUDY_NOT_FOUND",
                    $"Study directory not found: {studyInstanceUid}"));

            var files = Directory.EnumerateFiles(studyDir, "*" + StoragePaths.DicomExtension, SearchOption.AllDirectories)
                .ToList();

            if (files.Count == 0)
                return Result.Failure(new Error("STUDY_EMPTY",
                    $"Study {studyInstanceUid} has no DICOM files"));

            var corruptFiles = 0;
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileInfo = new FileInfo(file);
                if (fileInfo.Length == 0)
                {
                    corruptFiles++;
                    logger.LogWarning("Zero-byte DICOM file detected: {File}", file);
                    continue;
                }

                // Verify file is readable and compute checksum
                try
                {
                    await ChecksumCalculator.ComputeSha256Async(file, cancellationToken);
                }
                catch (Exception ex)
                {
                    corruptFiles++;
                    logger.LogWarning(ex, "Corrupt DICOM file detected: {File}", file);
                }
            }

            activity?.SetTag("storage.verify.total", files.Count);
            activity?.SetTag("storage.verify.corrupt", corruptFiles);

            if (corruptFiles > 0)
            {
                activity?.SetStatus(ActivityStatusCode.Error, $"{corruptFiles} corrupt files");
                return Result.Failure(new Error("INTEGRITY_FAILED",
                    $"{corruptFiles}/{files.Count} files failed integrity check"));
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogDebug("Integrity check passed for {StudyUid}: {Count} files OK",
                studyInstanceUid, files.Count);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Integrity check failed for {StudyUid}", studyInstanceUid);
            return Result.Failure(new Error("INTEGRITY_CHECK_FAILED", ex.Message));
        }
    }

    public async Task<Result<bool>> StudyExistsAsync(
        string studyInstanceUid, CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = await settings.GetStorageConfigAsync(cancellationToken);
            var studyDir = StoragePaths.StudyDirectory(cfg.RootPath, studyInstanceUid);
            var exists = Directory.Exists(studyDir)
                && Directory.EnumerateFiles(studyDir, "*" + StoragePaths.DicomExtension, SearchOption.AllDirectories).Any();
            return Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check study existence for {StudyUid}", studyInstanceUid);
            return Result<bool>.Failure(new Error("STORAGE_EXISTS_FAILED", ex.Message));
        }
    }

    public async Task<Result<bool>> InstanceExistsAsync(
        string sopInstanceUid, CancellationToken cancellationToken = default)
    {
        var pathResult = await GetInstancePathAsync(sopInstanceUid, cancellationToken);
        if (pathResult.IsFailure)
            return Result<bool>.Success(false);

        return Result<bool>.Success(File.Exists(pathResult.Value));
    }
}
