using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Constants;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        IPatientRepository patientRepository,
        ILogger<PatientsController> logger)
    {
        _patientRepository = patientRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var pagination = new PaginationRequest { Page = page, PageSize = pageSize };
        var result = await _patientRepository.GetPagedAsync(pagination, ct);
        return Ok(result.ToPagedResponse(p => p.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var patient = await _patientRepository.GetByIdAsync(id, ct);
        return patient is null ? NotFound() : Ok(patient.ToDto());
    }

    [HttpGet("by-dicom-id/{patientDicomId}")]
    public async Task<IActionResult> GetByDicomId(string patientDicomId, CancellationToken ct)
    {
        var patient = await _patientRepository.GetByPatientDicomIdAsync(patientDicomId, ct);
        return patient is null ? NotFound() : Ok(patient.ToDto());
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchByName([FromQuery] string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(HubApiConstants.NameQueryRequired);

        var patients = await _patientRepository.FindByNameAsync(name, ct);
        return Ok(patients.Select(p => p.ToDto()));
    }

    [HttpGet("by-node/{nodeId}")]
    public async Task<IActionResult> GetByNode(string nodeId, CancellationToken ct)
    {
        var patients = await _patientRepository.GetByNodeAsync(nodeId, ct);
        return Ok(patients.Select(p => p.ToDto()));
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var patients = await _patientRepository.GetActiveAsync(ct);
        return Ok(patients.Select(p => p.ToDto()));
    }

    [HttpGet("count")]
    public async Task<IActionResult> Count(CancellationToken ct)
    {
        var count = await _patientRepository.CountAsync(ct);
        return Ok(new CountDto { Count = count });
    }
}
