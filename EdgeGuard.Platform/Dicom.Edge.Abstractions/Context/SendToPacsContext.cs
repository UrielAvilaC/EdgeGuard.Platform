using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Context
{
    public sealed class SendToPacsContext
    {
        public string StudyInstanceUid { get; init; } = default!;
        public string DestinationAeTitle { get; init; } = default!;
        public string DestinationHost { get; init; } = default!;
        public int DestinationPort { get; init; }
    }
}
