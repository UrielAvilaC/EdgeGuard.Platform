using Dicom.Edge.Common.Filters;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Mapping;
using Dicom.Edge.Hub.Domain.Aggregates.Audit;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = Policies.ViewAuditLogs)]
public class AuditLogsController(
    IHubAuditLogRepository auditLogRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] AuditLogFilter filter, CancellationToken ct)
    {
        var pagination = new PaginationRequest { Page = filter.Page, PageSize = filter.PageSize };
        var criteria = new AuditLogFilterCriteria
        {
            EventType = filter.EventType,
            Severity = filter.Severity,
            UserId = filter.UserId,
            EntityType = filter.EntityType,
            EntityId = filter.EntityId,
            DateFrom = filter.DateFrom,
            DateTo = filter.DateTo,
            IsSuccess = filter.IsSuccess,
            Search = filter.Search,
            SortBy = filter.SortBy,
            SortDir = filter.SortDir
        };
        var result = await auditLogRepository.GetPagedAsync(pagination, criteria, ct);

        return Ok(result.ToPagedResponse(AuditLogMappingProfile.ToDto));
    }

    [HttpGet("by-entity/{entityType}/{entityId}")]
    public async Task<IActionResult> GetByEntity(string entityType, string entityId, CancellationToken ct)
    {
        var logs = await auditLogRepository.GetByEntityAsync(entityType, entityId, ct);
        return Ok(logs.Select(AuditLogMappingProfile.ToDto));
    }

    [HttpGet("by-correlation/{correlationId}")]
    public async Task<IActionResult> GetByCorrelation(string correlationId, CancellationToken ct)
    {
        var logs = await auditLogRepository.GetByCorrelationIdAsync(correlationId, ct);
        return Ok(logs.Select(AuditLogMappingProfile.ToDto));
    }

    [HttpGet("event-types")]
    public IActionResult GetEventTypes()
    {
        var types = Enum.GetNames<AuditEventType>();
        return Ok(types);
    }
}
