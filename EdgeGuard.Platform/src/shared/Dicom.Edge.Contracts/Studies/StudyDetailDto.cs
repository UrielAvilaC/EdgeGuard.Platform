using Dicom.Edge.Contracts.Dicom;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.Studies
{
    public class StudyDetailDto
    {
        public string StudyInstanceUid { get; set; } = default!;

        public string PatientId { get; set; } = default!;

        public string PatientName { get; set; } = default!;

        public DateTime StudyDate { get; set; }

        public string Modality { get; set; } = default!;

        public IReadOnlyCollection<DicomSeriesDto> Series { get; set; } = [];
    }
}
