# Shared Libraries

The EdgeGuard Platform uses a set of shared library projects that are referenced by both Hub and Node components. These libraries enforce consistency in cross-cutting concerns — abstractions, resilience, contracts, observability, and security — without creating circular dependencies.

---

## Overview

```
Dicom.Edge.Shared.Abstractions   ← Referenced by Application + Infrastructure
Dicom.Edge.Shared.Common         ← Referenced by Application + Infrastructure + API
Dicom.Edge.Shared.Contracts      ← Referenced by Hub.Api + Node.Api
Dicom.Edge.Shared.Diagnostics    ← Referenced by Hub.Diagnostics + Node.Diagnostics
Dicom.Edge.Shared.Models         ← Referenced by Domain + Application + Contracts
Dicom.Edge.Shared.Security       ← Referenced by Hub.Api + Node.Api
```

> **Note:** Shared libraries must never reference Hub-specific or Node-specific projects. They are the lowest layer in the dependency graph and may only reference other Shared libraries or .NET BCL types.

---

## Library Reference Table

| Library | Contents | Who Uses It | Rules |
|---|---|---|---|
| `Shared.Abstractions` | Repository and unit-of-work interfaces, audit and metrics contracts | `Hub.Application`, `Hub.Infrastructure`, `Hub.Persistence`, `Node.Persistence` | No EF Core, no ASP.NET — pure interfaces only |
| `Shared.Common` | Pagination helpers, `PagedResult<T>`, Polly resilience pipelines, guard clauses | `Hub.Application`, `Hub.Infrastructure`, `Node.Router`, `Node.Sender` | No domain types; no framework-specific dependencies |
| `Shared.Contracts` | DTOs, request/response models, Hub API route constants | `Hub.Api`, `Node.Api`, SPA TypeScript client generation | No business logic; data shapes only; must be serialisable |
| `Shared.Diagnostics` | Serilog setup, PHI redaction destructuring policies, OpenTelemetry, health check helpers | `Hub.Diagnostics`, `Node.Diagnostics` | Framework-aware (Serilog, OTEL) but no domain or persistence dependencies |
| `Shared.Models` | Shared enumerations: `StudyStatus`, `DicomModality`, `RoutingAction`, `Hl7MessageType` | `Hub.Domain`, `Hub.Application`, `Shared.Contracts`, `Node.Router` | Enums and simple value types only; no behaviour |
| `Shared.Security` | JWT extensions, permission policy definitions, role constants, middleware helpers | `Hub.Api`, `Node.Api` | ASP.NET Core dependency acceptable; no domain logic |

---

## Shared.Abstractions

`Shared.Abstractions` provides the contracts that allow Application and Domain layers to define their requirements without committing to any specific technology.

### Key Interfaces

```csharp
// Generic repository — implemented per aggregate in Persistence
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

// Unit of work — wraps a single database transaction
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// Audit logging — records mutations with actor context
public interface IAuditLogger
{
    Task LogAsync(string action, string entityType, Guid entityId,
                  object? before, object? after, string? actorId = null,
                  CancellationToken ct = default);
}

// Metrics collection — abstraction over OpenTelemetry counters/histograms
public interface IMetricsCollector
{
    void IncrementCounter(string name, KeyValuePair<string, object?>[]? tags = null);
    void RecordHistogram(string name, double value, KeyValuePair<string, object?>[]? tags = null);
}
```

### Usage Pattern

```csharp
// Application layer — depends only on the repository interface; no knowledge of EF.
// DTO mapping is done with hand-written extension methods — no mapping or mediator library.
public sealed class StudyService(IStudyRepository studyRepository, IUnitOfWork unitOfWork)
    : IStudyService
{
    public async Task<(Study? Study, string? Error)> UpdateStatusAsync(
        string id, UpdateStudyStatusRequest request, CancellationToken ct = default)
    {
        var study = await studyRepository.GetByIdAsync(id, ct);
        if (study is null) return (null, "Study not found");

        study.ChangeStatus(request.Status);          // raises a domain event
        await unitOfWork.SaveChangesAsync(ct);       // interceptor dispatches the event
        return (study, null);
    }
}
```

---

## Shared.Common

`Shared.Common` provides reusable utilities that are not tied to any domain concept.

### Pagination

```csharp
public record PaginationRequest(int Page = 1, int PageSize = 25)
{
    public int Skip => (Page - 1) * PageSize;
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

### Resilience Pipelines (Polly v8)

`Shared.Common` exposes pre-configured Polly `ResiliencePipeline` factories used by `Node.Sender` and `Hub.Infrastructure` for outbound HTTP and DICOM operations.

| Pipeline | Strategy | Configuration |
|---|---|---|
| `StandardRetry` | Retry | 3 attempts; exponential back-off (2s, 4s, 8s); jitter ±20% |
| `AggressiveRetry` | Retry | 5 attempts; fixed 1s delay; for DICOM C-STORE sends |
| `CircuitBreaker` | Circuit Breaker | Opens after 5 consecutive failures; 30s break duration |
| `NodePush` | Retry + Circuit Breaker | Combined pipeline for Hub→Node HTTP pushes |

```csharp
// Registration (called from Infrastructure DI extension)
services.AddResiliencePipeline("node-push", builder =>
{
    builder
        .AddRetry(ResiliencePipelineFactory.StandardRetryOptions())
        .AddCircuitBreaker(ResiliencePipelineFactory.DefaultCircuitBreakerOptions());
});
```

---

## Shared.Contracts

`Shared.Contracts` contains the data transfer objects (DTOs) and API route constants shared between the Hub API and the Node API. It is the only shared library that the frontend (SPA) TypeScript client generation targets.

### Key Contracts

| Namespace | Contents |
|---|---|
| `Contracts.Studies` | `StudyDto`, `StudySummaryDto`, `ReceiveStudyRequest`, `UpdateStudyStatusRequest` |
| `Contracts.Patients` | `PatientDto`, `MergePatientRequest` |
| `Contracts.Nodes` | `NodeDto`, `NodeRegistrationRequest`, `NodeHeartbeatRequest` |
| `Contracts.PacsDestinations` | `PacsDestinationDto`, `PacsDestinationSyncRequest` |
| `Contracts.RoutingRules` | `DicomRoutingRuleDto`, `DicomRoutingRuleSyncRequest` |
| `Contracts.Configuration` | `ConfigurationSyncRequest`, `NodeSettingsDto` |
| `Contracts.Routes` | `HubApiRoutes` (compile-time route constants) |

### Route Constants Pattern

```csharp
// Shared.Contracts — prevents string literal duplication
public static class HubApiRoutes
{
    public const string Studies      = "api/studies";
    public const string Patients     = "api/patients";
    public const string Nodes        = "api/nodes";
    public const string PacsServers  = "api/pacs-servers";
    public const string RoutingRules = "api/routing-rules";
    public const string EdgePrefix   = "api/edge";
}
```

---

## Shared.Diagnostics

`Shared.Diagnostics` provides a unified observability bootstrap consumed by both `Hub.Diagnostics` and `Node.Diagnostics`. It centralises Serilog configuration, PHI redaction, and OpenTelemetry setup so both components behave consistently.

### Serilog Bootstrap

```csharp
// Called at the very start of Program.cs before the host is built
SerilogBootstrap.Configure(args, "Hub");   // or "Node"
```

The bootstrap reads `Serilog` configuration from `appsettings.json` and applies the appropriate PHI redaction policy based on the component type.

### PHI Redaction

| Mode | Applied To | Behaviour |
|---|---|---|
| `Strict` | Edge Node | All patient identifiers (PatientName, PatientId, AccessionNumber, DOB, MRN) are replaced with `[REDACTED]` in every log sink |
| `Relaxed` | Hub | Structured log properties retain data for operational monitoring; free-text fields containing PHI are redacted |

### OpenTelemetry

`Shared.Diagnostics` registers:
- **Traces**: ASP.NET Core, HttpClient, EF Core, fo-dicom instrumentation sources
- **Metrics**: custom `IMetricsCollector` counters (studies received, routing rule evaluations, DICOM C-STORE outcomes)
- **Exporter**: OTLP exporter endpoint configurable per environment

### Health Checks

Pre-built health check registrations:

```csharp
services.AddEdgeHealthChecks(options =>
{
    options.AddDatabaseCheck();      // PostgreSQL or SQLite connectivity
    options.AddHubConnectivity();    // Node only: Hub HTTP reachability
    options.AddDicomServerCheck();   // Node only: DICOM SCP port open
    options.AddDiskSpaceCheck(      // Node only: DICOM storage free space
        minimumFreeMegabytes: 500);
});
```

---

## Shared.Models

`Shared.Models` provides the enumeration types shared across layers. No behaviour, no methods — just discriminated values.

| Enum | Values | Used By |
|---|---|---|
| `StudyStatus` | `Scheduled`, `Receiving`, `Received`, `Sending`, `Completed`, `Failed` | Domain, Application, Contracts, Node.Router |
| `DicomModality` | `CT`, `MR`, `CR`, `DX`, `US`, `NM`, `PT`, `RF`, `MG`, `OT`, ... (full DICOM modality list) | Domain, Node.Router, Contracts |
| `RoutingAction` | `SendToPacs`, `SendToHub`, `Anonymize`, `Discard` | Node.Router, Domain |
| `Hl7MessageType` | `ADT`, `ORM`, `ORU` | Infrastructure, Contracts |
| `Hl7TriggerEvent` | `A01`, `A08`, `A40`, `O01`, `R01` | Infrastructure |
| `Sex` | `Male`, `Female`, `Other`, `Unknown` | Domain, Contracts |

---

## Shared.Security

`Shared.Security` provides JWT helpers, role constants, and permission policy definitions used by both the Hub and Node APIs.

### Role Constants

```csharp
public static class EdgeRoles
{
    public const string Admin    = "Admin";
    public const string Operator = "Operator";
    public const string Viewer   = "Viewer";
}
```

### Permission Policies

```csharp
public static class EdgePolicies
{
    public const string ViewStudies    = nameof(ViewStudies);
    public const string ManageNodes    = nameof(ManageNodes);
    public const string ManageSettings = nameof(ManageSettings);
    public const string ManageUsers    = nameof(ManageUsers);
    public const string ViewAuditLogs  = nameof(ViewAuditLogs);
}
```

### JWT Extensions

`Shared.Security` exposes `IServiceCollection` extension methods for consistent JWT Bearer registration:

```csharp
// Applied in Hub.Api and Node.Api Program.cs
services.AddEdgeAuthentication(configuration);
services.AddEdgeAuthorization();
```

See [security-model.md](./security-model.md) for the full authentication and authorization architecture.
