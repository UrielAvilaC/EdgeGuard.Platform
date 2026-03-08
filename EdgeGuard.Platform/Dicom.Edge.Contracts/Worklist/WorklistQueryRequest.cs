using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.Worklist
{
    public class WorklistQueryRequest
    {
        public string? PatientId { get; set; }

        public string? AccessionNumber { get; set; }

        public string? Modality { get; set; }

        public DateTime? ScheduledDate { get; set; }
    }
}
