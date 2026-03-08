using Dicom.Edge.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Models.Transfer
{
    public class StudyTransfer
    {
        public string StudyInstanceUid { get; set; } = default!;

        public string EdgeNodeId { get; set; } = default!;

        public TransferStatus Status { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}
