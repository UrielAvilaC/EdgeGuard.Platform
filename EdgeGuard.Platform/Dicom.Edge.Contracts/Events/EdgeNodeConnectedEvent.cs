using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.Events
{
    public class EdgeNodeConnectedEvent
    {
        public string EdgeNodeId { get; set; } = default!;

        public DateTime ConnectedAt { get; set; }

        public string IpAddress { get; set; } = default!;
    }
}
