using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Context
{
    public sealed class DicomReceiveContext
    {
        public string StudyInstanceUid { get; set; } = default!;
        public string SeriesInstanceUid { get; set; } = default!;
        public string SopInstanceUid { get; set; } = default!;
        public string Modality { get; set; } = default!;
        public string CallingAeTitle { get; set; } = default!;
        public string FilePath { get; set; } = default!;
    }
}
