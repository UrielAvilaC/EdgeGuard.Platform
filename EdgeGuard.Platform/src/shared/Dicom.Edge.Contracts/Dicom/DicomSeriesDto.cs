using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.Dicom
{
    public class DicomSeriesDto
    {
        public string SeriesInstanceUid { get; set; } = default!;

        public string StudyInstanceUid { get; set; } = default!;

        public string Modality { get; set; } = default!;

        public int InstanceCount { get; set; }
    }
}
