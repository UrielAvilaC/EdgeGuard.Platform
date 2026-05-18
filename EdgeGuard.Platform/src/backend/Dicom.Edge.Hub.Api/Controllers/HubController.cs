using Dicom.Edge.Hub.Api.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/hub")]
[EnableRateLimiting("api")]
public class HubController : ControllerBase
{
    [HttpGet("runtime")]
    public IActionResult GetRuntime()
    {
        return Ok(EdgeMappingProfile.ToHubRuntime());
    }
}
