using Dicom.Edge.Abstractions.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Routing
{
    public interface IRouter
    {
        Task<RouteDecision> DecideAsync(StudyContext context);
    }
}
