using Dicom.Edge.Common.Errors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Common.Results
{
    public class ValidationResult : Result
    {
        public ValidationResult(List<ValidationError> errors)
            : base(ResultStatus.Invalid)
        {
            ValidationErrors = errors;
        }
    }
}
