using Dicom.Edge.Common.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Queue
{
    public interface IEdgeQueue<T>
    {
        Task<Result> Queue (T item);
        Task<Result> Queue (IEnumerable<T> items);
        Task<Result<T?>> DequeueAsync(CancellationToken cancellationToken = default);
    }
}
