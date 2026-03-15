using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Security.Audit
{
    public class AuditEvent
    {
        public string EventType { get; set; } = default!;

        public string UserId { get; set; } = default!;

        public DateTime Timestamp { get; set; }

        public string Description { get; set; } = default!;
    }
}
