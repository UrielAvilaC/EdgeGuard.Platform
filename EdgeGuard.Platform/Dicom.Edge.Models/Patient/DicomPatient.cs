using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Models.Patient
{
    public class DicomPatient
    {
        public string PatientId { get; set; } = default!;

        public string PatientName { get; set; } = default!;

        public DateTime? BirthDate { get; set; }

        public string Sex { get; set; } = default!;
    }
}
