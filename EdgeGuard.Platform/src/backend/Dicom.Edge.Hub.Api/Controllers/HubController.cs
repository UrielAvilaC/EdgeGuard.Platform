using Dicom.Edge.Hub.Api.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HubController : ControllerBase
{
    [HttpGet("runtime")]
    public IActionResult GetRuntime()
    {
        return Ok(new
        {
            service = HubApiConstants.ServiceName,
            utcNow = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable(HubApiConstants.EnvironmentVariableName)
                          ?? HubApiConstants.DefaultEnvironment,
            machineName = Environment.MachineName,
            framework = Environment.Version.ToString()
        });
    }
}
