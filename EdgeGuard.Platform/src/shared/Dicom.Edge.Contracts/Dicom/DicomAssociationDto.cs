namespace Dicom.Edge.Contracts.Dicom
{
    /// <summary>
    /// Data transfer object for DICOM association information.
    /// </summary>
    public class DicomAssociationDto
    {
        public string Id { get; set; } = default!;
        public string CallingAeTitle { get; set; } = default!;
        public string CalledAeTitle { get; set; } = default!;
        public string RemoteIpAddress { get; set; } = default!;
        public int RemotePort { get; set; }
        public DateTime ConnectedAt { get; set; }
        public DateTime? DisconnectedAt { get; set; }
        public string Status { get; set; } = default!;
        public int ImagesReceived { get; set; }
        public long BytesReceived { get; set; }
        public string? RejectionReason { get; set; }
        public double? ThroughputMbps { get; set; }
        public double? ConnectionDurationSeconds { get; set; }
    }
}
