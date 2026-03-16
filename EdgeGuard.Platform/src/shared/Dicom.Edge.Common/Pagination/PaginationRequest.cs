using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Common.Pagination
{
    public class PaginationRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 25;

        public int Skip => (Page - 1) * PageSize;
    }
}
