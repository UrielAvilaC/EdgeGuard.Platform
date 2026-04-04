using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudiesController : ControllerBase
{
    private readonly IStudyRepository _studyRepository;

    public StudiesController(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var pagination = new PaginationRequest { Page = page, PageSize = pageSize };
        var result = await _studyRepository.GetPagedAsync(pagination, ct);
        return Ok(new
        {
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            Items = result.Items.Select(MapToDto)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var study = await _studyRepository.GetByIdAsync(id, ct);
        return study is null ? NotFound() : Ok(MapToDto(study));
    }

    [HttpGet("by-uid/{studyInstanceUid}")]
    public async Task<IActionResult> GetByUid(string studyInstanceUid, CancellationToken ct)
    {
        var study = await _studyRepository.GetByStudyInstanceUidAsync(studyInstanceUid, ct);
        return study is null ? NotFound() : Ok(MapToDto(study));
    }

    [HttpGet("by-patient/{patientId}")]
    public async Task<IActionResult> GetByPatient(string patientId, CancellationToken ct)
    {
        var studies = await _studyRepository.GetByPatientIdAsync(patientId, ct);
        return Ok(studies.Select(MapToDto));
    }

    [HttpGet("by-node/{nodeId}")]
    public async Task<IActionResult> GetByNode(string nodeId, CancellationToken ct)
    {
        var studies = await _studyRepository.GetByNodeAsync(nodeId, ct);
        return Ok(studies.Select(MapToDto));
    }

    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetByStatus(StudyStatus status, CancellationToken ct)
    {
        var studies = await _studyRepository.GetByStatusAsync(status, ct);
        return Ok(studies.Select(MapToDto));
    }

    [HttpGet("by-date")]
    public async Task<IActionResult> GetByDateRange([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        var studies = await _studyRepository.GetByDateRangeAsync(from, to, ct);
        return Ok(studies.Select(MapToDto));
    }

    [HttpGet("pending-pacs")]
    public async Task<IActionResult> GetPendingForPacs(CancellationToken ct)
    {
        var studies = await _studyRepository.GetPendingForPacsAsync(ct);
        return Ok(studies.Select(MapToDto));
    }

    [HttpGet("count")]
    public async Task<IActionResult> Count(CancellationToken ct)
    {
        var count = await _studyRepository.CountAsync(ct);
        return Ok(new { count });
    }

    private static object MapToDto(Study s) => new
    {
        s.Id,
        StudyInstanceUid = s.StudyInstanceUid.Value,
        s.AccessionNumber,
        s.StudyDate,
        s.StudyDescription,
        s.ReferringPhysician,
        s.PatientId,
        s.PatientName,
        s.SourceNodeId,
        s.SourceAeTitle,
        Status = s.Status.ToString(),
        s.InstanceCount,
        s.SeriesCount,
        s.TotalSizeBytes,
        s.FirstImageReceivedAt,
        s.LastImageReceivedAt,
        s.Priority,
        s.IsUrgent,
        s.TargetPacsId,
        s.SentToPacsAt,
        s.PacsSendAttempts,
        s.CreatedAt,
        s.UpdatedAt,
        SeriesCount2 = s.Series.Count
    };
}
