using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Models.Storage
{
    public class StorageLocation
    {
        public string RootPath { get; set; } = default!;

        public long MaxStorageMb { get; set; }

        public bool IsArchive { get; set; }
    }
}
