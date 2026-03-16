using Dicom.Edge.Common.Results;

namespace Dicom.Edge.Abstractions.Storage
{
    /// <summary>
    /// Provides abstraction for DICOM file storage operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports multiple storage backends: local filesystem, network shares, cloud storage.
    /// Implementations handle path resolution, storage management, and integrity verification.
    /// </para>
    /// </remarks>
    public interface IStorageProvider
    {
        // ==================== Path Resolution ====================

        /// <summary>
        /// Gets the file system path for a study.
        /// </summary>
        /// <param name="studyInstanceUid">Study Instance UID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The full path to the study directory.</returns>
        Task<Result<string>> GetStudyPathAsync(string studyInstanceUid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the file system path for a DICOM instance.
        /// </summary>
        /// <param name="sopInstanceUid">SOP Instance UID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The full path to the DICOM file.</returns>
        Task<Result<string>> GetInstancePathAsync(string sopInstanceUid, CancellationToken cancellationToken = default);

        // ==================== Storage Management ====================

        /// <summary>
        /// Gets available storage space in bytes.
        /// </summary>
        Task<Result<long>> GetAvailableSpaceAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total storage capacity in bytes.
        /// </summary>
        Task<Result<long>> GetTotalSpaceAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates the total size of a study in bytes.
        /// </summary>
        /// <param name="studyInstanceUid">Study Instance UID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result<long>> GetStudySizeAsync(string studyInstanceUid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all studies in storage.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result<IEnumerable<string>>> ListStudiesAsync(CancellationToken cancellationToken = default);

        // ==================== File Operations ====================

        /// <summary>
        /// Deletes a study and all its instances from storage.
        /// </summary>
        /// <param name="studyInstanceUid">Study Instance UID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result> DeleteStudyAsync(string studyInstanceUid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a specific DICOM instance.
        /// </summary>
        /// <param name="sopInstanceUid">SOP Instance UID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result> DeleteInstanceAsync(string sopInstanceUid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Archives a study to long-term storage.
        /// </summary>
        /// <param name="studyInstanceUid">Study Instance UID.</param>
        /// <param name="archivePath">Archive destination path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result> ArchiveStudyAsync(
            string studyInstanceUid,
            string archivePath,
            CancellationToken cancellationToken = default);

        // ==================== Integrity & Validation ====================

        /// <summary>
        /// Verifies the integrity of a study (checksums, file existence).
        /// </summary>
        /// <param name="studyInstanceUid">Study Instance UID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result> VerifyIntegrityAsync(string studyInstanceUid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a study exists in storage.
        /// </summary>
        /// <param name="studyInstanceUid">Study Instance UID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result<bool>> StudyExistsAsync(string studyInstanceUid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if an instance exists in storage.
        /// </summary>
        /// <param name="sopInstanceUid">SOP Instance UID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result<bool>> InstanceExistsAsync(string sopInstanceUid, CancellationToken cancellationToken = default);
    }
}
