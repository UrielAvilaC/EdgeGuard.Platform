using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.Edge
{
    public class EdgeNodeDto
    {
        public string Id { get; set; } = default!;

        public string Name { get; set; } = default!;

        public string AETitle { get; set; } = default!;

        public string IpAddress { get; set; } = default!;

        public int Port { get; set; }

        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
