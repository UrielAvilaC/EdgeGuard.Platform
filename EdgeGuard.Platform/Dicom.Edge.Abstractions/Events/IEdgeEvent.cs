using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Events
{
    public interface IEdgeEvent
    {
        Guid EventId { get; }

        DateTime OccurredAtUtc { get; }

        string EventType { get; }

        string Source { get; }

        string? CorrelationId { get; }

        int Version { get; }
    }
}
