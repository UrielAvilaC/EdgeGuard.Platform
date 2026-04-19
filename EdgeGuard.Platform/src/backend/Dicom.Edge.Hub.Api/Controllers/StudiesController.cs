using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.CsvServices;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Models.Enums;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.ViewStudies)]
public class StudiesController : ControllerBase
{
    private readonly IStudyRepository _studyRepository;
    private readonly ICsvExportService _csvExportService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StudiesController> _logger;

    public StudiesController(
        IStudyRepository studyRepository,
        ICsvExportService csvExportService,
        IUnitOfWork unitOfWork,
        ILogger<StudiesController> logger)
    {
        _studyRepository = studyRepository;
        _csvExportService = csvExportService;
        _unitOfWork = unitOfWork;
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

    [HttpPut("{id}/status")]
    [Authorize(Policy = Policies.EditStudyMetadata)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStudyStatusRequest request, CancellationToken ct)
    {
        var study = await _studyRepository.GetByIdAsync(id, ct);
        if (study is null) return NotFound();

        if (!Enum.TryParse<StudyStatus>(request.Status, true, out var newStatus))
            return BadRequest(new ErrorDto { Error = $"Invalid status: {request.Status}" });

        switch (newStatus)
        {
            case StudyStatus.Completed: study.MarkCompleted(); break;
            case StudyStatus.Failed: study.MarkFailed(request.Reason ?? "Manual status change"); break;
            case StudyStatus.SentToPacs: study.MarkSentToPacs(); break;
            default:
                return BadRequest(new ErrorDto { Error = $"Manual transition to {newStatus} is not supported" });
        }

        await _studyRepository.UpdateAsync(study, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Study {StudyId} status manually changed to {Status}", id, newStatus);
        return Ok(study.ToDto());
    }

    [HttpGet("export")]
    [Authorize(Policy = Policies.ExportStudies)]
    public async Task<IActionResult> Export([FromQuery] StudyFilter filter, CancellationToken ct)
    {
        var result = await _csvExportService.ExportStudiesAsync(filter, ct);
        return File(result.FileContent, result.ContentType, result.FileName);
    }
}
