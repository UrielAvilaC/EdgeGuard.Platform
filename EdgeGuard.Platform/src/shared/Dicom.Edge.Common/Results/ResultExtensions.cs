using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Common.Results
{
    public static class ResultExtensions
    {
        public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper)
        {
            if (result.IsFailure)
                return Result<TOut>.Failure(result.Error!);

            return Result<TOut>.Success(mapper(result.Value));
        }
    }
}
