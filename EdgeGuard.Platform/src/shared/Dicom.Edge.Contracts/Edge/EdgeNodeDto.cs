namespace Dicom.Edge.Contracts.Edge
{
    /// <summary>
    /// Data transfer object for Edge Node information.
    /// </summary>
    public class EdgeNodeDto
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string AETitle { get; set; } = default!;
        public string IpAddress { get; set; } = default!;
        public int Port { get; set; }
        public string? ApiEndpoint { get; set; }

        // Status
        public bool IsOnline { get; set; }
        public string Status { get; set; } = default!;
        public DateTime LastSeen { get; set; }
        public string? Version { get; set; }

        // Location
        public string? Location { get; set; }
        public string? FacilityName { get; set; }
        public string? TimeZone { get; set; }

        // Capacity
        public long MaxStorageMb { get; set; }
        public long AvailableStorageMb { get; set; }
        public double StorageUsagePercent { get; set; }

        // Statistics
        public int QueuedStudies { get; set; }
        public int TotalStudiesReceived { get; set; }
        public int TotalStudiesSent { get; set; }
        public double TransferSuccessRate { get; set; }
        public int ErrorsLast24Hours { get; set; }

        // Contact
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }

        public DateTime RegisteredAt { get; set; }
    }
}
