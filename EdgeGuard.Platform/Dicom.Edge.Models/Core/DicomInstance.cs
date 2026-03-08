using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Models.Core
{
    public class DicomInstance
    {
        public string SopInstanceUid { get; set; } = default!;

        public string SeriesInstanceUid { get; set; } = default!;

        public string SopClassUid { get; set; } = default!;

        public int InstanceNumber { get; set; }

        public string FilePath { get; set; } = default!;
    }
}
