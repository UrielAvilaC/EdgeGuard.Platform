using Dicom.Edge.Common.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Storage
{
    public interface IStorageProvider
    {
        Task<Result<string>> GetStudyPathAsync(string studyInstanceUid, CancellationToken cancellationToken = default);

        Task<Result<string>> GetInstancePathAsync(string sopInstanceUid, CancellationToken cancellationToken = default);
    }
}
