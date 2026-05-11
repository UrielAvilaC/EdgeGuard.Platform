using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Routing
{
    public sealed class RouteDestination
    {
        public string AeTitle { get; init; } = default!;

        public string Host { get; init; } = default!;

        public int Port { get; init; }

        public bool UseTls { get; init; }

        public string? Description { get; init; }

        public override string ToString()
        {
            return $"{AeTitle}@{Host}:{Port}";
        }
    }
}
