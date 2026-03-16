using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.Events
{
    public class StudyStoredEvent
    {
        public string StudyInstanceUid { get; set; } = default!;

        public string EdgeNodeId { get; set; } = default!;

        public DateTime StoredAt { get; set; }
    }
}
