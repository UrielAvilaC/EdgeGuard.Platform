using Dicom.Edge.Abstractions.Context;
using Dicom.Edge.Common.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Abstractions.Dicom
{
    public interface IDicomAssociationHandler
    {
        Task<Result> OnAssociationRequestAsync(AssociationRequestContext context, CancellationToken cancellationToken = default);
    }
}
