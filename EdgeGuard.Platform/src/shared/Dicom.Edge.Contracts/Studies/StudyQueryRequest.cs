using Dicom.Edge.Contracts.Common;

namespace Dicom.Edge.Contracts.Studies
{
    public class StudyQueryRequest : PagedRequest
    {
        public string? PatientId { get; set; }

        public string? PatientName { get; set; }

        public string? Modality { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
