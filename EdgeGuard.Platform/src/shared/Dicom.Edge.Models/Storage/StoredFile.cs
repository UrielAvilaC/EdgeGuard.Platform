using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Models.Storage
{
    public class StoredFile
    {
        public string FileId { get; set; } = default!;

        public string StudyInstanceUid { get; set; } = default!;

        public string FilePath { get; set; } = default!;

        public long SizeBytes { get; set; }

        public DateTime StoredAt { get; set; }
    }
}
