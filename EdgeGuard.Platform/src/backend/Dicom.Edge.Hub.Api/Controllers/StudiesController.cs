using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.CsvServices;
using Dicom.Edge.Hub.Application.Studies;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/studies")]
[Authorize(Policy = Policies.ViewStudies)]
public class StudiesController : ControllerBase
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyService _studyService;
    private readonly ICsvExportService _csvExportService;
    private readonly ILogger<StudiesController> _logger;

    public StudiesController(
        IStudyRepository studyRepository,
        IStudyService studyService,
        ICsvExportService csvExportService,
        ILogger<StudiesController> logger)
    {
        _studyRepository = studyRepository;
        _studyService = studyService;
        _csvExportService = csvExportService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] StudyFilter filter, CancellationToken ct = default)
    {
        var pagination = new PaginationRequest { Page = filter.Page, PageSize = filter.PageSize };
        var criteria = new StudyFilterCriteria
        {
            Search = filter.Search,
            Status = filter.Status,
            SourceNodeId = filter.SourceNodeId,
            PatientId = filter.PatientId,
            DateFrom = filter.DateFrom,
            DateTo = filter.DateTo,
            IsUrgent = filter.IsUrgent,
            SortBy = filter.SortBy,
            SortDir = filter.SortDir
        };
        var result = await _studyRepository.GetFilteredPagedAsync(pagination, criteria, ct);
        return Ok(result.ToPagedResponse(s => s.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var study = await _studyRepository.GetByIdAsync(id, ct);
        return study is null ? NotFound() : Ok(study.ToDto());
    }

    [HttpGet("by-uid/{studyInstanceUid}")]
    public async Task<IActionResult> GetByUid(string studyInstanceUid, CancellationToken ct)
    {
        var study = await _studyRepository.GetByStudyInstanceUidAsync(studyInstanceUid, ct);
        return study is null ? NotFound() : Ok(study.ToDto());
    }

    [HttpGet("by-patient/{patientId}")]
    public async Task<IActionResult> GetByPatient(string patientId, CancellationToken ct)
    {
        var studies = await _studyRepository.GetByPatientIdAsync(patientId, ct);
        return Ok(studies.Select(s => s.ToDto()));
    }

    [HttpGet("by-node/{nodeId}")]
    public async Task<IActionResult> GetByNode(string nodeId, CancellationToken ct)
    {
        var studies = await _studyRepository.GetByNodeAsync(nodeId, ct);
        return Ok(studies.Select(s => s.ToDto()));
    }

    [HttpGet("pending-pacs")]
    public async Task<IActionResult> GetPendingForPacs(CancellationToken ct)
    {
        var studies = await _studyRepository.GetPendingForPacsAsync(ct);
        return Ok(studies.Select(s => s.ToDto()));
    }

    [HttpGet("count")]
    public async Task<IActionResult> Count(CancellationToken ct)
    {
        var count = await _studyRepository.CountAsync(ct);
        return Ok(new CountDto { Count = count });
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.EditStudyMetadata)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateStudyRequest request, CancellationToken ct)
    {
        var study = await _studyService.UpdateAsync(id, request, ct);
        return study is null ? NotFound() : Ok(study.ToDto());
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = Policies.EditStudyMetadata)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStudyStatusRequest request, CancellationToken ct)
    {
        var (study, error) = await _studyService.UpdateStatusAsync(id, request, ct);

        if (error is not null)
            return BadRequest(new ErrorDto { Error = error });

        return study is null ? NotFound() : Ok(study.ToDto());
    }

    [HttpGet("export")]
    [Authorize(Policy = Policies.ExportStudies)]
    public async Task<IActionResult> Export([FromQuery] StudyFilter filter, CancellationToken ct)
    {
        var result = await _csvExportService.ExportStudiesAsync(filter, ct);
        return File(result.FileContent, result.ContentType, result.FileName);
    }
}
