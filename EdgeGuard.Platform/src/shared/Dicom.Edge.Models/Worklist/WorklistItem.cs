using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Models.Worklist
{
    public class WorklistItem
    {
        public string AccessionNumber { get; set; } = default!;

        public string PatientId { get; set; } = default!;

        public string PatientName { get; set; } = default!;

        public DateTime ScheduledDate { get; set; }

        public string Modality { get; set; } = default!;

        public string ProcedureDescription { get; set; } = default!;
    }
}
