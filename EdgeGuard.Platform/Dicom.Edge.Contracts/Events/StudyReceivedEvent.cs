using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.Events
{
    public class StudyReceivedEvent
    {
        public string StudyInstanceUid { get; set; } = default!;

        public string EdgeNodeId { get; set; } = default!;

        public DateTime ReceivedAt { get; set; }
    }
}
