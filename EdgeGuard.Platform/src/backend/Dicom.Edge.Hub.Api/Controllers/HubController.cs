using Dicom.Edge.Hub.Api.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HubController : ControllerBase
{
    [HttpGet("runtime")]
    public IActionResult GetRuntime()
    {
        return Ok(EdgeMappingProfile.ToHubRuntime());
    }
}
