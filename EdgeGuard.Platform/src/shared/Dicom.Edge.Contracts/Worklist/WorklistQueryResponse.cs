using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Contracts.Worklist
{
    public class WorklistQueryResponse
    {
        public IReadOnlyCollection<WorklistItemDto> Items { get; set; } = [];
    }
}
