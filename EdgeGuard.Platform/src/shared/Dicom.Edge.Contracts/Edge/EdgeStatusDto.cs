using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.Edge
{
    public class EdgeStatusDto
    {
        public string EdgeNodeId { get; set; } = default!;

        public bool IsOnline { get; set; }

        public DateTime LastHeartbeat { get; set; }

        public int PendingStudies { get; set; }

        public long StorageUsedMb { get; set; }
    }
}
