using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Constants;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.CsvServices;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.ViewStudies)]
public class PatientsController : ControllerBase
{
    private readonly IPatientRepository _patientRepository;
    private readonly ICsvExportService _csvExportService;
    private readonly ICsvImportService _csvImportService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        IPatientRepository patientRepository,
        ICsvExportService csvExportService,
        ICsvImportService csvImportService,
        IUnitOfWork unitOfWork,
        ILogger<PatientsController> logger)
    {
        _patientRepository = patientRepository;
        _csvExportService = csvExportService;
        _csvImportService = csvImportService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PatientFilter filter, CancellationToken ct = default)
    {
        var pagination = new PaginationRequest { Page = filter.Page, PageSize = filter.PageSize };
        var criteria = new PatientFilterCriteria
        {
            Search = filter.Search,
            CreatedByNodeId = filter.CreatedByNodeId,
            IsActive = filter.IsActive,
            HasPhone = filter.HasPhone,
            HasEmail = filter.HasEmail,
            SortBy = filter.SortBy,
            SortDir = filter.SortDir
        };
        var result = await _patientRepository.GetFilteredPagedAsync(pagination, criteria, ct);
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

    [HttpGet("count")]
    public async Task<IActionResult> Count(CancellationToken ct)
    {
        var count = await _patientRepository.CountAsync(ct);
        return Ok(new CountDto { Count = count });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePatientRequest request, CancellationToken ct)
    {
        var patient = await _patientRepository.GetByIdAsync(id, ct);
        if (patient is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.PatientName))
            patient.UpdateDemographics(request.PatientName, request.BirthDate, request.Sex);

        patient.UpdateContactInfo(request.PhoneNumber, request.Email);

        await _patientRepository.UpdateAsync(patient, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Ok(patient.ToDto());
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] PatientFilter filter, CancellationToken ct)
    {
        var result = await _csvExportService.ExportPatientsAsync(filter, ct);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ErrorDto { Error = "CSV file is required." });

        using var stream = file.OpenReadStream();
        var result = await _csvImportService.ImportPatientsAsync(stream, ct);
        return Ok(result);
    }
}
