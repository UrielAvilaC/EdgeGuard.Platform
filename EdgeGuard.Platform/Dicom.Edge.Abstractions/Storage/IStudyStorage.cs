using Dicom.Edge.Abstractions.Context;
using Dicom.Edge.Common.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Storage
{
    public interface IStudyStorage
    {
        Task<Result> SaveInstanceAsync(DicomReceiveContext context);
        Task<Result> MarkStudyCompletedAsync(StudyContext context);
    }
}
