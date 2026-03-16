using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.System
{
    public class HealthStatusDto
    {
        public string Status { get; set; } = default!;

        public DateTime Timestamp { get; set; }

        public string Version { get; set; } = default!;
    }
}
