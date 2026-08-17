using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Context
{
    public sealed class StudyContext
    {
        public string StudyInstanceUid { get; set; } = default!;
        public int InstanceCount { get; set; }
        public string CallingAeTitle { get; set; } = default!;
        public DateTime CompletedAt { get; set; }

        // ── Patient / study metadata for Hub notification ──────────────────────
        public string? PatientId { get; set; }
        public string? PatientName { get; set; }
        public DateTime? PatientBirthDate { get; set; }
        public string? PatientSex { get; set; }
        public string? AccessionNumber { get; set; }
        public long TotalSizeBytes { get; set; }
        public DateTime? StudyDate { get; set; }
        public string? StudyDescription { get; set; }
        public int SeriesCount { get; set; }
    }
}
