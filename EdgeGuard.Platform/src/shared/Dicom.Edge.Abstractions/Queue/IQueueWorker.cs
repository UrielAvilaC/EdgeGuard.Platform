using Dicom.Edge.Common.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Queue
{
    public interface IQueueWorker
    {
        Task<Result> StartAsync(CancellationToken cancellationToken = default);
        Task<Result> StopAsync(CancellationToken cancellationToken = default);
    }
}
