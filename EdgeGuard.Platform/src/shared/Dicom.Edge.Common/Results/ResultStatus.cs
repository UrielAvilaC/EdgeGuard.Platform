using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Common.Results
{
    public enum ResultStatus
    {
        Success,
        Failure,
        Invalid,
        NotFound,
        Unauthorized,
        Forbidden,
        Conflict
    }
}
