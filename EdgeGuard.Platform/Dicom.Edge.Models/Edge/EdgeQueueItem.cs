using Dicom.Edge.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Models.Edge
{
    public class EdgeQueueItem
    {
        public string StudyInstanceUid { get; set; } = default!;

        public TransferStatus Status { get; set; }

        public int RetryCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastAttempt { get; set; }
    }
}
