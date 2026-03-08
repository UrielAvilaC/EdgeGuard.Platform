using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Extensions
{
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult(this Result result)
        {
            return ResultMapper.Map(result);
        }

        public static IActionResult ToActionResult<T>(this Result<T> result)
        {
            if (result.IsSuccess)
                return new OkObjectResult(result.Value);

            return ResultMapper.Map(result);
        }
    }
}
