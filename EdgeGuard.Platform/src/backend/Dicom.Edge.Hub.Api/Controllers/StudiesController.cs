using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Contracts.Notifications;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.CsvServices;
using Dicom.Edge.Hub.Application.Notifications;
using Dicom.Edge.Hub.Application.Reports;
using Dicom.Edge.Hub.Application.Studies;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Models.Enums;
using Dicom.Edge.Security.Authorization;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/studies")]
[Authorize(Policy = Policies.ViewStudies)]
[EnableRateLimiting("api")]
public class StudiesController : ControllerBase
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyService _studyService;
    private readonly ICsvExportService _csvExportService;
    private readonly IReportStorage _reportStorage;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly ILogger<StudiesController> _logger;

    public StudiesController(
        IStudyRepository studyRepository,
        IStudyService studyService,
        ICsvExportService csvExportService,
        IReportStorage reportStorage,
        IQrCodeGenerator qrCodeGenerator,
        IHtmlSanitizer htmlSanitizer,
        ILogger<StudiesController> logger)
    {
        _studyRepository = studyRepository;
        _studyService = studyService;
        _csvExportService = csvExportService;
        _reportStorage = reportStorage;
        _qrCodeGenerator = qrCodeGenerator;
        _htmlSanitizer = htmlSanitizer;
        _logger = logger;
    }

    // ── Diagnostic report (ORU results) ──────────────────────────────────────

    /// <summary>GET /api/studies/{id}/report — report metadata + sanitized content + links.</summary>
    [HttpGet("{id}/report")]
    public async Task<IActionResult> GetReport(string id, CancellationToken ct)
    {
        var study = await _studyRepository.GetByIdAsync(id, ct);
        if (study is null) return NotFound();

        var content = study.ReportContent;
        if (study.ReportFormat == ReportFormat.Html && !string.IsNullOrEmpty(content))
            content = _htmlSanitizer.Sanitize(content);

        return Ok(new ReportDto
        {
            StudyId = study.Id,
            Status = study.Status.ToString(),
            ReportFormat = study.ReportFormat.ToString(),
            Content = content,
            HasPdf = !string.IsNullOrEmpty(study.ReportPdfPath),
            ImageLinks = SplitLinks(study.ExternalImageLinks)
        });
    }

    /// <summary>GET /api/studies/{id}/report/pdf — streams the stored report PDF.</summary>
    [HttpGet("{id}/report/pdf")]
    public async Task<IActionResult> GetReportPdf(string id, CancellationToken ct)
    {
        var study = await _studyRepository.GetByIdAsync(id, ct);
        if (study?.ReportPdfPath is null) return NotFound();

        var stream = await _reportStorage.OpenPdfAsync(study.ReportPdfPath, ct);
        if (stream is null) return NotFound();

        return File(stream, "application/pdf", $"report-{study.Id}.pdf");
    }

    /// <summary>GET /api/studies/{id}/report/qr — PNG QR code of the image link.</summary>
    [HttpGet("{id}/report/qr")]
    public async Task<IActionResult> GetReportQr(string id, [FromQuery] string? link, CancellationToken ct)
    {
        var target = link;
        if (string.IsNullOrWhiteSpace(target))
        {
            var study = await _studyRepository.GetByIdAsync(id, ct);
            if (study is null) return NotFound();
            target = SplitLinks(study.ExternalImageLinks).FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(target)) return NotFound();

        var png = _qrCodeGenerator.GeneratePng(target);
        return File(png, "image/png");
    }

    private static IReadOnlyList<string> SplitLinks(string? links) =>
        string.IsNullOrEmpty(links)
            ? []
            : links.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    // ── Results delivery ─────────────────────────────────────────────────────

    /// <summary>POST /api/studies/{id}/deliver — enqueue results delivery (email/WhatsApp).</summary>
    [HttpPost("{id}/deliver")]
    [Authorize(Policy = Policies.EditStudyMetadata)]
    public async Task<IActionResult> Deliver(
        string id, [FromBody] DeliverResultsRequest request,
        [FromServices] IDeliveryService deliveryService, CancellationToken ct)
    {
        var result = await deliveryService.DeliverAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>GET /api/studies/{id}/deliveries — delivery history for the study.</summary>
    [HttpGet("{id}/deliveries")]
    public async Task<IActionResult> GetDeliveries(
        string id, [FromServices] IDeliveryService deliveryService, CancellationToken ct) =>
        Ok(await deliveryService.GetHistoryAsync(id, ct));

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
