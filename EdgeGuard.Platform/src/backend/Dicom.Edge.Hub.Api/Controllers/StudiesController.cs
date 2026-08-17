using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Contracts.Notifications;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Application.CsvServices;
using Dicom.Edge.Hub.Application.Notifications;
using Dicom.Edge.Hub.Application.Reports;
using Dicom.Edge.Hub.Application.Studies;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
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
    private readonly IPatientRepository _patientRepository;
    private readonly IStudyService _studyService;
    private readonly ICsvExportService _csvExportService;
    private readonly IReportStorage _reportStorage;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly ILogger<StudiesController> _logger;

    public StudiesController(
        IStudyRepository studyRepository,
        IPatientRepository patientRepository,
        IStudyService studyService,
        ICsvExportService csvExportService,
        IReportStorage reportStorage,
        IQrCodeGenerator qrCodeGenerator,
        IHtmlSanitizer htmlSanitizer,
        ILogger<StudiesController> logger)
    {
        _studyRepository = studyRepository;
        _patientRepository = patientRepository;
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
        // Operator-initiated from the study detail screen.
        var result = await deliveryService.DeliverAsync(
            id, request, NotificationTriggerSource.Manual, ct);
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

        // The SPA filters by patient record id; older callers pass an MRN. Resolve both.
        var (patientRecordId, patientDicomId) = string.IsNullOrWhiteSpace(filter.PatientId)
            ? (null, null)
            : await ResolvePatientKeysAsync(filter.PatientId, ct);

        var criteria = new StudyFilterCriteria
        {
            Search = filter.Search,
            Status = filter.Status,
            SourceNodeId = filter.SourceNodeId,
            PatientId = patientDicomId,
            PatientRecordId = patientRecordId,
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

    /// <summary>GET /api/studies/{id}/infrastructure — origin node + target PACS identity and connectivity.</summary>
    [HttpGet("{id}/infrastructure")]
    public async Task<IActionResult> GetInfrastructure(
        string id, [FromServices] IStudyInfrastructureService infrastructureService, CancellationToken ct)
    {
        var dto = await infrastructureService.GetAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("by-uid/{studyInstanceUid}")]
    public async Task<IActionResult> GetByUid(string studyInstanceUid, CancellationToken ct)
    {
        var study = await _studyRepository.GetByStudyInstanceUidAsync(studyInstanceUid, ct);
        return study is null ? NotFound() : Ok(study.ToDto());
    }

    /// <summary>
    /// GET /api/studies/by-patient/{patientId} — studies of a patient. Accepts either the
    /// patient record id (<c>patients.id</c>, what the SPA holds) or the DICOM Patient ID
    /// (MRN), so existing callers keep working.
    /// </summary>
    [HttpGet("by-patient/{patientId}")]
    public async Task<IActionResult> GetByPatient(string patientId, CancellationToken ct)
    {
        var (recordId, dicomId) = await ResolvePatientKeysAsync(patientId, ct);

        var studies = recordId is null
            ? await _studyRepository.GetByPatientIdAsync(dicomId!, ct)
            : await _studyRepository.GetByPatientAsync(recordId, dicomId, ct);

        return Ok(studies.Select(s => s.ToDto()));
    }

    /// <summary>
    /// Maps whatever identifier the caller supplied to the (record id, MRN) pair used by
    /// the study queries. A value that matches no patient record is treated as an MRN.
    /// </summary>
    private async Task<(string? RecordId, string? DicomId)> ResolvePatientKeysAsync(
        string patientId, CancellationToken ct)
    {
        var byRecordId = await _patientRepository.GetByIdAsync(patientId, ct);
        if (byRecordId is not null)
            return (byRecordId.Id, byRecordId.PatientDicomId.Value);

        var byDicomId = await _patientRepository.GetByPatientDicomIdAsync(patientId, ct);
        return byDicomId is not null
            ? (byDicomId.Id, byDicomId.PatientDicomId.Value)
            : (null, patientId);
    }

    [HttpGet("by-node/{nodeId}")]
    public async Task<IActionResult> GetByNode(string nodeId, CancellationToken ct)
    {
        var studies = await _studyRepository.GetByNodeAsync(nodeId, ct);
        return Ok(studies.Select(s => s.ToDto()));
    }

    /// <summary>POST /api/studies/{id}/requeue — manual resend of a Failed study to chosen PACS.</summary>
    [HttpPost("{id}/requeue")]
    [Authorize(Policy = Policies.EditStudyMetadata)]
    public async Task<IActionResult> Requeue(
        string id, [FromBody] RequeueStudyRequest request,
        [FromServices] IStudyResendService resendService, CancellationToken ct)
    {
        var result = await resendService.RequeueAsync(id, request.PacsIds, ct);

        if (result.NotFound) return NotFound();
        if (!result.Accepted) return BadRequest(new ErrorDto { Error = result.Error ?? "Resend failed." });

        var study = await _studyRepository.GetByIdAsync(id, ct);
        return Ok(study?.ToDto());
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
