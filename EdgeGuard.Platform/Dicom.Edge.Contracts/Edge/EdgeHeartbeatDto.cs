using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.Edge
{
    public class EdgeHeartbeatDto
    {
        public string EdgeNodeId { get; set; } = default!;

        public DateTime Timestamp { get; set; }

        public int QueueSize { get; set; }

        public long DiskUsageMb { get; set; }
    }
}
