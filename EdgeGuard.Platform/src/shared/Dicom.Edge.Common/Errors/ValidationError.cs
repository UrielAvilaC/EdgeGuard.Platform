using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Common.Errors
{
    public record ValidationError(
     string Field,
     string Message
 );
}
