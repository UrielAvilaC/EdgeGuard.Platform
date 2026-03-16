namespace Dicom.Edge.Contracts.Studies
{
    /// <summary>
    /// Data transfer object for DICOM study information.
    /// </summary>
    public class StudyDto
    {
        // Core DICOM identifiers
        public string StudyInstanceUid { get; set; } = default!;
        public string PatientId { get; set; } = default!;
        public string PatientName { get; set; } = default!;
        public DateTime StudyDate { get; set; }

        // Study details
        public string? StudyDescription { get; set; }
        public string? AccessionNumber { get; set; }
        public string? ReferringPhysician { get; set; }
        public string Modality { get; set; } = default!;

        // Counts
        public int SeriesCount { get; set; }
        public int InstanceCount { get; set; }

        // Status & Tracking
        public string Status { get; set; } = default!;
        public long TotalSizeBytes { get; set; }
        public double SizeMB { get; set; }

        // Timestamps
        public DateTime ReceivedAt { get; set; }
        public DateTime LastImageReceivedAt { get; set; }
        public DateTime? SentToHubAt { get; set; }
        public DateTime? ArchivedAt { get; set; }

        // Source
        public string SourceAeTitle { get; set; } = default!;
        public string EdgeNodeId { get; set; } = default!;

        // Error tracking
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
    }
}
