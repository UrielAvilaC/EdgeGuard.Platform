using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.Common
{
    public class PagedRequest
    {
        private const int MaxPageSize = 100;

        public int Page { get; set; } = 1;

        private int _pageSize = 25;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public string? Search { get; set; }

        public string? SortBy { get; set; }

        public SortDirection SortDirection { get; set; } = SortDirection.Desc;
    }
}
