using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Security.Identity
{
    public class EdgeIdentity
    {
        public string NodeId { get; set; } = default!;

        public string AETitle { get; set; } = default!;

        public string IpAddress { get; set; } = default!;
    }
}
