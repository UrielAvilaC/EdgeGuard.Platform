using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Models.Core
{
    public class DicomSeries
    {
        public string SeriesInstanceUid { get; set; } = default!;

        public string StudyInstanceUid { get; set; } = default!;

        public string Modality { get; set; } = default!;

        public int InstanceCount { get; set; }

        public List<DicomInstance> Instances { get; set; } = new();
    }
}
