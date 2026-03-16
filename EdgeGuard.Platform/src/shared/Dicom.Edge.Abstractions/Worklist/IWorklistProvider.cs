using Dicom.Edge.Common.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Worklist
{
    public interface IWorklistProvider
    {
        Task<Result<IEnumerable<object>>> QueryAsync(object request);
    }
}
