using Dicom.Edge.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Mappers
{
    public static class ResultMapper
    {
        public static IActionResult Map(Result result)
        {
            return result.Status switch
            {
                ResultStatus.Success => new OkResult(),

                ResultStatus.Invalid => new BadRequestObjectResult(
                    result.ValidationErrors),

                ResultStatus.NotFound => new NotFoundObjectResult(
                    result.Error),

                ResultStatus.Unauthorized => new UnauthorizedObjectResult(
                    result.Error),

                ResultStatus.Forbidden => new ObjectResult(result.Error)
                {
                    StatusCode = StatusCodes.Status403Forbidden
                },

                ResultStatus.Conflict => new ConflictObjectResult(
                    result.Error),

                _ => new ObjectResult(result.Error)
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                }
            };
        }
    }
}
