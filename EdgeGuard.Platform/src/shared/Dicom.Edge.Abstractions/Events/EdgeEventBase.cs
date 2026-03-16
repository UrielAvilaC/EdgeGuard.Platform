using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Events
{
    public abstract record EdgeEventBase : IEdgeEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();

        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;

        public string EventType => GetType().Name;

        public string Source { get; init; } = "Dicom.Edge.Node";

        public string? CorrelationId { get; init; }

        public int Version { get; init; } = 1;
    }
}
