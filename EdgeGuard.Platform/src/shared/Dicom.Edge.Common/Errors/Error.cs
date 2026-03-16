using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Common.Errors
{
    public sealed record Error
    {
        public string Code { get; }

        public string Message { get; }

        public static readonly Error None = new(string.Empty, string.Empty);

        public Error(string code, string message)
        {
            Code = code;
            Message = message;
        }
    }
}
