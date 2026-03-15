using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Models.Storage
{
    /// <summary>
    /// Represents an archived study record for long-term retention and cleanup management.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Archiving enables:
    /// <list type="bullet">
    ///   <item><description>Freeing active storage for new studies</description></item>
    ///   <item><description>Maintaining records for compliance/audit</description></item>
    ///   <item><description>Scheduled deletion after retention period</description></item>
    ///   <item><description>Disaster recovery and study retrieval</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Typical Retention Policies:</strong>
    /// <list type="bullet">
    ///   <item><description>Emergency studies: 7-14 days</description></item>
    ///   <item><description>Routine studies: 30-90 days</description></item>
    ///   <item><description>Research studies: 1-7 years</description></item>
    ///   <item><description>Legal holds: Indefinite until released</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class StudyArchive
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StudyInstanceUid { get; set; } = default!;
        
        /// <summary>
        /// UTC timestamp when study was archived.
        /// </summary>
        public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Physical or cloud storage location of archived study.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Examples:</strong>
        /// <list type="bullet">
        ///   <item><description>"file:///mnt/archive/2024/01/study123.zip"</description></item>
        ///   <item><description>"s3://my-bucket/archives/study123.tar.gz"</description></item>
        ///   <item><description>"azure://container/study123.zip"</description></item>
        ///   <item><description>"tape://backup-system/volume5/study123"</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string ArchiveLocation { get; set; } = default!;
        
        /// <summary>
        /// Size of archived data in bytes.
        /// </summary>
        public long SizeBytes { get; set; }
        
        /// <summary>
        /// UTC timestamp when study is scheduled for permanent deletion.
        /// </summary>
        /// <remarks>
        /// Null = No deletion scheduled (retained indefinitely).
        /// </remarks>
        public DateTime? DeleteScheduledAt { get; set; }
        
        /// <summary>
        /// Whether the study has been permanently deleted.
        /// </summary>
        public bool IsDeleted { get; set; }
        
        /// <summary>
        /// UTC timestamp when study was permanently deleted.
        /// </summary>
        public DateTime? DeletedAt { get; set; }
        
        /// <summary>
        /// Reason for archiving.
        /// </summary>
        public ArchiveReason Reason { get; set; }
        
        /// <summary>
        /// User ID who initiated archival (for manual archives).
        /// </summary>
        public string? ArchivedBy { get; set; }
        
        /// <summary>
        /// MD5 or SHA256 checksum of archived data for integrity verification.
        /// </summary>
        public string? Checksum { get; set; }
        
        /// <summary>
        /// Whether archive data is encrypted.
        /// </summary>
        public bool IsEncrypted { get; set; }
        
        /// <summary>
        /// Compression ratio achieved (original size / compressed size).
        /// </summary>
        public double? CompressionRatio { get; set; }
        
        /// <summary>
        /// Additional notes about the archive.
        /// </summary>
        public string? Notes { get; set; }
        
        /// <summary>
        /// Whether this study is under legal hold (cannot be deleted).
        /// </summary>
        public bool IsLegalHold { get; set; }
        
        /// <summary>
        /// Gets size in megabytes for display.
        /// </summary>
        public double SizeMB => SizeBytes / (1024.0 * 1024.0);
        
        /// <summary>
        /// Gets days until scheduled deletion.
        /// </summary>
        public int? DaysUntilDeletion =>
            DeleteScheduledAt.HasValue && !IsDeleted
                ? (int)(DeleteScheduledAt.Value - DateTime.UtcNow).TotalDays
                : null;
    }
}
