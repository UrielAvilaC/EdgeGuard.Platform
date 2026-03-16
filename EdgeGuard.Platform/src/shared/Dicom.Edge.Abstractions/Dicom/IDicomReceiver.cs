using Dicom.Edge.Abstractions.Context;
using Dicom.Edge.Common.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Dicom
{
    public interface IDicomReceiver
    {
        Task<Result> OnInstanceReceivedAsync(DicomReceiveContext context, CancellationToken cancellationToken = default);
        Task<Result> OnStudyCompletedAsync(StudyContext  context,  CancellationToken cancellationToken = default);
    }
}