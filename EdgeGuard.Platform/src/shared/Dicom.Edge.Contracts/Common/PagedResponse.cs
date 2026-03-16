using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.Common
{
    public class PagedResponse<T>
    {
        public IReadOnlyCollection<T> Items { get; init; } = [];

        public int Page { get; init; }

        public int PageSize { get; init; }

        public int TotalCount { get; init; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
