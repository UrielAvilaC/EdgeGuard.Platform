using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Routing
{
    public sealed class RouteDecision
    {
        public bool ShouldRoute { get; }

        public IReadOnlyList<RouteDestination> Destinations { get; }

        public string? Reason { get; }

        private RouteDecision(
            bool shouldRoute,
            IReadOnlyList<RouteDestination> destinations,
            string? reason)
        {
            ShouldRoute = shouldRoute;
            Destinations = destinations;
            Reason = reason;
        }

        public static RouteDecision NoRoute(string reason)
        {
            return new RouteDecision(
                false,
                Array.Empty<RouteDestination>(),
                reason);
        }

        public static RouteDecision RouteTo(
            params RouteDestination[] destinations)
        {
            return new RouteDecision(
                true,
                destinations,
                null);
        }

        public static RouteDecision RouteTo(
            IEnumerable<RouteDestination> destinations)
        {
            return new RouteDecision(
                true,
                [.. destinations],
                null);
        }
    }
}
