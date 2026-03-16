using Dicom.Edge.Common.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Security
{
    public interface IAuthProvider
    {
        Task<Result> ValidateAeAsync(string aeTitle, string ip);
    }
}
