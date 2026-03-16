using Dicom.Edge.Models.Enums;
using Dicom.Edge.Models.Patient;

namespace Dicom.Edge.Models.Core
{
    /// <summary>
    /// Represents a complete DICOM study with comprehensive metadata for Edge Node processing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A DICOM study is a collection of one or more series acquired during a single imaging session.
    /// This class extends the basic DICOM study model with enterprise-level tracking, auditing,
    /// and lifecycle management fields required for production medical imaging systems.
    /// </para>
    /// <para>
    /// <strong>DICOM Hierarchy:</strong>
    /// <code>
    /// Patient
    ///   └── Study (this class)
    ///         └── Series
    ///               └── Instance (Image)
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Lifecycle States:</strong>
    /// <list type="number">
    ///   <item><description><strong>Receiving:</strong> Images are actively being transferred</description></item>
    ///   <item><description><strong>Completed:</strong> All expected images received</description></item>
    ///   <item><description><strong>QueuedForSend:</strong> Ready to send to destination</description></item>
    ///   <item><description><strong>Sending:</strong> Currently transmitting to Hub/PACS</description></item>
    ///   <item><description><strong>SentToPacs:</strong> Successfully transmitted</description></item>
    ///   <item><description><strong>Failed:</strong> Error occurred during processing</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class DicomStudy
    {
        // ==================== Core DICOM Identifiers ====================

        /// <summary>
        /// Gets or sets the unique Study Instance UID (0020,000D).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Globally unique identifier for this study as defined by DICOM standard.
        /// Format: OID (e.g., "1.2.840.113619.2.55.3.604688119.868.1234567890.123")
        /// </para>
        /// <para>
        /// <strong>Primary Key:</strong> Use this as the primary key in databases.
        /// </para>
        /// </remarks>
        public string StudyInstanceUid { get; set; } = default!;

        /// <summary>
        /// Gets or sets the Patient ID (0010,0020) from DICOM metadata.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Hospital/facility-specific patient identifier.
        /// May not be globally unique (different facilities may reuse IDs).
        /// </para>
        /// <para>
        /// For privacy-sensitive systems, consider storing hashed/tokenized version.
        /// </para>
        /// </remarks>
        public string PatientId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the Patient Name (0010,0010) in DICOM format.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Format: "LastName^FirstName^MiddleName^Prefix^Suffix"
        /// Example: "Doe^John^A^Dr^Jr"
        /// </para>
        /// <para>
        /// <strong>Privacy Note:</strong> PHI (Protected Health Information) - handle according to HIPAA/GDPR.
        /// </para>
        /// </remarks>
        public string PatientName { get; set; } = default!;

        /// <summary>
        /// Gets or sets the date when the study was performed (0008,0020).
        /// </summary>
        /// <remarks>
        /// DICOM Study Date tag. Use for chronological sorting and reporting.
        /// </remarks>
        public DateTime StudyDate { get; set; }

        // ==================== Status and Tracking ====================

        /// <summary>
        /// Gets or sets the current processing status of this study.
        /// </summary>
        /// <remarks>
        /// See <see cref="StudyStatus"/> for lifecycle states and transitions.
        /// </remarks>
        public StudyStatus Status { get; set; } = StudyStatus.Receiving;

        /// <summary>
        /// Gets or sets the total number of DICOM instances (images) in this study.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Updated as images are received. Use to:
        /// <list type="bullet">
        ///   <item><description>Track reception progress</description></item>
        ///   <item><description>Validate study completeness</description></item>
        ///   <item><description>Estimate storage requirements</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public int InstanceCount { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the last image was received.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Updated with each incoming instance.
        /// Use to detect stalled transfers (e.g., no images received in 5 minutes).
        /// </para>
        /// </remarks>
        public DateTime LastImageReceivedAt { get; set; }

        // ==================== Enterprise Fields (NEW) ====================

        /// <summary>
        /// Gets or sets the UTC timestamp when the first image of this study was received.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use for:
        /// <list type="bullet">
        ///   <item><description>Reception performance metrics</description></item>
        ///   <item><description>Calculating total reception time</description></item>
        ///   <item><description>Audit trail</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the Application Entity Title of the source modality that sent this study.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Identifies which modality/system sent the study (e.g., "CT_SCANNER_1", "MR_UNIT_A").
        /// Critical for:
        /// <list type="bullet">
        ///   <item><description>Routing decisions (route based on source)</description></item>
        ///   <item><description>Security auditing</description></item>
        ///   <item><description>Quality metrics per modality</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string SourceAeTitle { get; set; } = default!;

        /// <summary>
        /// Gets or sets the ID of the Edge Node that received this study.
        /// </summary>
        /// <remarks>
        /// Useful in multi-node deployments for:
        /// <list type="bullet">
        ///   <item><description>Load balancing analysis</description></item>
        ///   <item><description>Node-specific troubleshooting</description></item>
        ///   <item><description>Geographic routing decisions</description></item>
        /// </list>
        /// </remarks>
        public string EdgeNodeId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the total size of all instances in bytes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Sum of all DICOM file sizes.
        /// Use for:
        /// <list type="bullet">
        ///   <item><description>Storage capacity planning</description></item>
        ///   <item><description>Transfer bandwidth requirements</description></item>
        ///   <item><description>Cost allocation (cloud storage)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Example Calculation:</strong>
        /// <code>
        /// TotalSizeBytes = Series.SelectMany(s => s.Instances).Sum(i => i.FileSizeBytes);
        /// </code>
        /// </para>
        /// </remarks>
        public long TotalSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the Study Description (0008,1030) from DICOM metadata.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Free-text description of the study (e.g., "CT Chest with Contrast").
        /// Useful for:
        /// <list type="bullet">
        ///   <item><description>User interfaces and reporting</description></item>
        ///   <item><description>Routing rules (route based on description patterns)</description></item>
        ///   <item><description>Search and filtering</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? StudyDescription { get; set; }

        /// <summary>
        /// Gets or sets the Accession Number (0008,0050) from DICOM metadata.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Hospital/RIS-specific identifier linking study to order/procedure.
        /// Important for:
        /// <list type="bullet">
        ///   <item><description>Correlating with Worklist (MWL) queries</description></item>
        ///   <item><description>Billing integration</description></item>
        ///   <item><description>Order tracking</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? AccessionNumber { get; set; }

        /// <summary>
        /// Gets or sets the Referring Physician Name (0008,0090).
        /// </summary>
        /// <remarks>
        /// Name of physician who ordered the study. May be required for regulatory compliance.
        /// </remarks>
        public string? ReferringPhysician { get; set; }

        // ==================== Transfer Tracking ====================

        /// <summary>
        /// Gets or sets the UTC timestamp when the study was successfully sent to the Hub.
        /// </summary>
        /// <remarks>
        /// Null if not yet sent or send failed.
        /// </remarks>
        public DateTime? SentToHubAt { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the study was archived to long-term storage.
        /// </summary>
        /// <remarks>
        /// Null if not yet archived. Archived studies may be deleted from active storage.
        /// </remarks>
        public DateTime? ArchivedAt { get; set; }

        /// <summary>
        /// Gets or sets the error message if processing failed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Populated when Status = Failed.
        /// Should contain actionable information for troubleshooting.
        /// </para>
        /// <para>
        /// <strong>Example:</strong>
        /// "Failed to send to Hub: Connection timeout after 3 retries"
        /// </para>
        /// </remarks>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the number of times transfer was retried.
        /// </summary>
        /// <remarks>
        /// Incremented on each retry attempt. Use for:
        /// <list type="bullet">
        ///   <item><description>Detecting persistent failures</description></item>
        ///   <item><description>Escalation triggers (e.g., alert after 5 retries)</description></item>
        ///   <item><description>Reliability metrics</description></item>
        /// </list>
        /// </remarks>
        public int RetryCount { get; set; }

        // ==================== Relationships ====================

        /// <summary>
        /// Gets or sets the patient associated with this study.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Navigation property for ORM frameworks.
        /// Contains additional patient demographics and metadata.
        /// </para>
        /// </remarks>
        public DicomPatient? Patient { get; set; }

        /// <summary>
        /// Gets or sets the collection of series in this study.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A study contains one or more series (e.g., Localizer, Axial slices, Coronal slices).
        /// Each series contains multiple instances (images).
        /// </para>
        /// </remarks>
        public List<DicomSeries> Series { get; set; } = new();

        // ==================== Computed Properties ====================

        /// <summary>
        /// Gets the total number of series in this study.
        /// </summary>
        public int SeriesCount => Series?.Count ?? 0;

        /// <summary>
        /// Gets whether the study has been successfully sent to the Hub.
        /// </summary>
        public bool IsSentToHub => SentToHubAt.HasValue;

        /// <summary>
        /// Gets whether the study has been archived.
        /// </summary>
        public bool IsArchived => ArchivedAt.HasValue;

        /// <summary>
        /// Gets the size of the study in megabytes for display purposes.
        /// </summary>
        public double SizeMB => TotalSizeBytes / (1024.0 * 1024.0);

        /// <summary>
        /// Gets the duration of the reception process.
        /// </summary>
        /// <remarks>
        /// Null if study is still receiving. Otherwise: LastImageReceivedAt - ReceivedAt
        /// </remarks>
        public TimeSpan? ReceptionDuration =>
            Status == StudyStatus.Receiving ? null : LastImageReceivedAt - ReceivedAt;
    }
}
