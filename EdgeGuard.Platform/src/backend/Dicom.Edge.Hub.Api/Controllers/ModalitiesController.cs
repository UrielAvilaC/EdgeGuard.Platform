using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Domain.Aggregates.Modalities;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dicom.Edge.Hub.Api.Controllers;

/// <summary>
/// Exposes the global modality reference catalog. Used by the UI to populate the
/// equipment modality picker — pass <c>supportedOnly=true</c> to restrict to
/// image-level, assignable modalities.
/// </summary>
[ApiController]
[Route("api/modalities")]
[Authorize(Policy = Policies.ViewConfiguration)]
[EnableRateLimiting("api")]
public sealed class ModalitiesController(IModalityRepository modalityRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool supportedOnly = false, CancellationToken ct = default)
    {
        var modalities = await modalityRepository.GetAllAsync(supportedOnly, ct);
        return Ok(modalities.Select(m => m.ToDto()));
    }
}
