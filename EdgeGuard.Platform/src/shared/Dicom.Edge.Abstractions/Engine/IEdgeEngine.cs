using Dicom.Edge.Common.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Engine
{
    public interface IEdgeEngine
    {
        Task<Result> StartAsync(CancellationToken cancellationToken = default);
        Task<Result> StopAsync(CancellationToken cancellationToken = default);
    }
}
