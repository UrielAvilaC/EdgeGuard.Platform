using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Models.Core
{
    public class DicomStudy
    {
        public string StudyInstanceUid { get; set; } = default!;

        public string PatientId { get; set; } = default!;

        public string PatientName { get; set; } = default!;

        public DateTime StudyDate { get; set; }

        public StudyStatus Status { get; set; } = StudyStatus.Receiving;

        public int InstanceCount { get; set; }

        public DateTime LastImageReceivedAt { get; set; }

        public List<DicomSeries> Series { get; set; } = new();
    }
}
