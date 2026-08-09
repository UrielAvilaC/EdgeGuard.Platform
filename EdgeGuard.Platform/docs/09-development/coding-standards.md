# Coding Standards

This document defines the coding standards for EdgeGuard Platform. All new code must follow these standards; existing code should be updated toward these standards when touched.

---

## Table of Contents

1. [C# Standards](#c-standards)
   - [Nullable Reference Types](#nullable-reference-types)
   - [Async/Await](#asyncawait)
   - [Domain Aggregate Conventions](#domain-aggregate-conventions)
   - [Naming Conventions](#naming-conventions)
   - [XML Documentation](#xml-documentation)
2. [Angular/TypeScript Standards](#angulartypescript-standards)
   - [Components](#components)
   - [State Management](#state-management)
   - [File Naming](#file-naming)
3. [Error Handling](#error-handling)
4. [General Rules](#general-rules)

---

## C# Standards

### Nullable Reference Types

Nullable reference types are **enabled** in all projects (`<Nullable>enable</Nullable>` in `.csproj`).

**Rules:**
- Never use `null!` (null-forgiving) unless you have documented proof that a value cannot be null at that point.
- Prefer `string?` over `string` where null is a valid state.
- Initialize all non-nullable properties in constructors or use required properties.
- Use `ArgumentNullException.ThrowIfNull()` for public API guard clauses.

```csharp
// Correct
public sealed class Study : AggregateRoot<StudyId>
{
    private Study() { } // EF Core private constructor

    public DicomUid StudyInstanceUid { get; private set; } = null!; // Set by Create()
    public string? AccessionNumber { get; private set; }            // Nullable — optional

    public static Study Create(DicomUid uid, PatientId patientId)
    {
        ArgumentNullException.ThrowIfNull(uid);
        ArgumentNullException.ThrowIfNull(patientId);
        // ...
    }
}

// Wrong — never use #nullable disable
#nullable disable
public class BadExample { }
```

### Async/Await

All I/O-bound operations must be `async`/`await`. Never use `.Result` or `.Wait()` on Tasks in application code.

```csharp
// Correct
public async Task<Study?> GetByUidAsync(DicomUid uid, CancellationToken ct = default)
{
    return await _context.Studies
        .FirstOrDefaultAsync(s => s.StudyInstanceUid == uid, ct);
}

// Wrong — blocks the thread
public Study? GetByUid(DicomUid uid)
{
    return _context.Studies
        .FirstOrDefaultAsync(s => s.StudyInstanceUid == uid)
        .Result; // NEVER
}
```

**Naming:** Async methods must have the `Async` suffix.

**CancellationToken:** Always accept and propagate `CancellationToken` in async methods in repositories and application services.

### Domain Aggregate Conventions

1. **No public constructors.** Aggregates are created via a static `Create()` factory method that enforces invariants.
2. **Private setters on all properties.** State is modified only through domain methods.
3. **Raise domain events** via `AddDomainEvent()` when significant state changes occur.
4. **No public setters for collections.** Expose `IReadOnlyList<T>`.

```csharp
// Correct aggregate pattern
public sealed class Patient : AggregateRoot<PatientId>, ISoftDeletable
{
    private readonly List<Study> _studies = [];

    // Private parameterless constructor for EF Core
    private Patient() { }

    public PatientIdentifier Identifier { get; private set; } = null!;
    public string FullName { get; private set; } = string.Empty;
    public IReadOnlyList<Study> Studies => _studies.AsReadOnly();

    // ISoftDeletable
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Creates a new Patient and raises a <see cref="PatientRegisteredEvent"/>.</summary>
    public static Patient Create(PatientIdentifier identifier, string fullName)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        var patient = new Patient
        {
            Identifier = identifier,
            FullName = fullName
        };

        patient.AddDomainEvent(new PatientRegisteredEvent(patient.Id, identifier));
        return patient;
    }

    public void MergeFrom(Patient sourcePatient)
    {
        ArgumentNullException.ThrowIfNull(sourcePatient);
        // Domain logic here
    }
}
```

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Types (class, struct, record, enum) | PascalCase | `StudyStatusAudit` |
| Methods | PascalCase | `GetByIdAsync` |
| Properties | PascalCase | `StudyInstanceUid` |
| Public constants | PascalCase | `MaxRetryAttempts` |
| Local variables | camelCase | `studyId` |
| Method parameters | camelCase | `cancellationToken` |
| Private fields | `_camelCase` | `_studyRepository` |
| Interfaces | `I` prefix + PascalCase | `IStudyRepository` |
| Async methods | `Async` suffix | `GetStudiesAsync` |
| Generic type parameters | `T` prefix | `TId`, `TResult` |

### XML Documentation

All public types, methods, and properties must have XML documentation comments.

```csharp
/// <summary>
/// Represents a DICOM Study within the system.
/// </summary>
/// <remarks>
/// A Study is the primary aggregate root for tracking DICOM data.
/// Studies transition through a defined status lifecycle managed by
/// <see cref="TransitionStatus"/>.
/// </remarks>
public sealed class Study : AggregateRoot<StudyId>
{
    /// <summary>Gets the unique DICOM Study Instance UID for this study.</summary>
    public DicomUid StudyInstanceUid { get; private set; } = null!;

    /// <summary>
    /// Transitions the study status to the specified target status.
    /// </summary>
    /// <param name="targetStatus">The desired next status.</param>
    /// <param name="reason">Optional reason for the transition.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the transition is not allowed by the status state machine.
    /// </exception>
    public void TransitionStatus(StudyStatus targetStatus, string? reason = null)
    {
        // ...
    }
}
```

---

## Angular/TypeScript Standards

### Components

- **Standalone components only.** Do not use NgModules for new components.
- **OnPush change detection** on all components — no exceptions.
- **No direct DOM manipulation.** Never use `document.querySelector`, `ElementRef.nativeElement`, or `Renderer2` for business logic. Use Angular template bindings.
- **No ViewChild for DOM refs** unless interfacing with a third-party library that requires it.

```typescript
// Correct
@Component({
  selector: 'app-study-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './study-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudyListComponent {
  // ...
}

// Wrong — NgModule-based
@NgModule({ declarations: [StudyListComponent] })
export class StudyModule { }
```

### State Management

Use Angular Signals for component state. Prefer `signal()`, `computed()`, and `effect()` over `BehaviorSubject` for new code.

```typescript
// Correct — Signals
@Component({ /* ... */ })
export class StudyDetailComponent {
  protected readonly study = signal<Study | null>(null);
  protected readonly studyTitle = computed(() =>
    this.study()?.accessionNumber ?? 'Loading...'
  );

  loadStudy(id: string): void {
    this.studyService.getById(id).subscribe(study => this.study.set(study));
  }
}

// Acceptable for cross-component observable streams (services)
@Injectable({ providedIn: 'root' })
export class StudySignalRService {
  private readonly _studyUpdated$ = new Subject<StudyUpdated>();
  readonly studyUpdated$ = this._studyUpdated$.asObservable();
}

// Avoid for new component-local state
// private readonly studies$ = new BehaviorSubject<Study[]>([]); // old pattern
```

### File Naming

Angular files follow kebab-case convention:

| Type | Naming | Example |
|------|--------|---------|
| Component | `feature.component.ts` | `study-detail.component.ts` |
| Component template | `feature.component.html` | `study-detail.component.html` |
| Component styles | `feature.component.scss` | `study-detail.component.scss` |
| Service | `feature.service.ts` | `study.service.ts` |
| Model/Interface | `feature.model.ts` | `study.model.ts` |
| Route | `feature.routes.ts` | `studies.routes.ts` |
| Guard | `feature.guard.ts` | `auth.guard.ts` |
| Pipe | `feature.pipe.ts` | `study-status.pipe.ts` |

TypeScript interfaces in Angular use PascalCase for the type name, stored in `.model.ts` files:

```typescript
// studies/models/study.model.ts
export interface Study {
  id: string;
  studyInstanceUid: string;
  status: StudyStatus;
  accessionNumber: string | null;
}

export type StudyStatus = 'Pending' | 'Scheduled' | 'Received' | 'Sending' | 'Sent' | 'Failed';
```

---

## Error Handling

### Application layer — Result pattern

Application services return `Result<T>` instead of throwing exceptions for business rule failures.

```csharp
// Application service — returns Result
public async Task<Result<StudyDto>> GetStudyAsync(StudyId id, CancellationToken ct)
{
    var study = await _repository.GetByIdAsync(id, ct);
    if (study is null)
        return Result.Failure<StudyDto>(StudyErrors.NotFound(id));

    return Result.Success(_mapper.Map<StudyDto>(study));
}

// Controller — converts Result to HTTP response
[HttpGet("{id}")]
public async Task<IActionResult> GetStudy(string id, CancellationToken ct)
{
    var result = await _studyService.GetStudyAsync(StudyId.From(id), ct);
    return result.IsSuccess
        ? Ok(result.Value)
        : result.ToProblemDetails(HttpContext);  // Maps to ProblemDetails (RFC 7807)
}
```

### API layer — ProblemDetails

All API error responses use RFC 7807 `ProblemDetails` format:

```json
{
  "type": "https://edgeguard.example.com/errors/study-not-found",
  "title": "Study Not Found",
  "status": 404,
  "detail": "Study with ID 'study-abc123' was not found.",
  "traceId": "00-abc123def456-01"
}
```

### Never swallow exceptions

```csharp
// Wrong — exception swallowed
try
{
    await _service.DoSomethingAsync();
}
catch (Exception)
{
    // Silent fail — never do this
}

// Correct — log and re-throw or convert to Result
try
{
    await _service.DoSomethingAsync();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    _logger.LogError(ex, "Unexpected error in {Operation}", nameof(DoSomethingAsync));
    throw;
}
```

---

## General Rules

- **No magic numbers or strings.** Use named constants or enums.
- **No `var` when the type is not obvious** from the right-hand side.
- **Single responsibility.** Methods do one thing. Classes have one reason to change.
- **Keep methods short.** If a method exceeds 40 lines, consider splitting it.
- **Favor immutability.** Value objects are always immutable. Records over classes for DTOs.
- **No static mutable state.** Singleton services via DI only, no static fields that are written at runtime.
- **Logging levels:**
  - `Debug` — diagnostic details useful during development
  - `Information` — significant operational events (study received, HL7 processed)
  - `Warning` — unexpected conditions that are recoverable
  - `Error` — failures requiring attention (PACS send failed, DB connection lost)
  - `Fatal/Critical` — unrecoverable errors causing shutdown

### NuGet packages — Central Package Management

Package versions are managed centrally in [Directory.Packages.props](../../Directory.Packages.props).

- A `.csproj` declares **only** the package: `<PackageReference Include="Serilog.Sinks.File" />`.
  A `Version` attribute in a `.csproj` is a build error under CPM — put the version in
  `Directory.Packages.props` instead.
- Adding a package = one `<PackageReference>` in the project + one `<PackageVersion>` in the
  central file (grouped by the existing sections).
- Upgrading = a single edit in the central file; every project moves together, so versions
  cannot drift between projects.
- Transitive pinning is **enabled**. A `<PackageVersion>` for a package nobody references
  directly forces that version on the restore graph — this is how vulnerable transitive
  dependencies get patched (see the last ItemGroup in the file). Do not add a fake
  `<PackageReference>` for that purpose.
- Before releasing, check the graph:

```bash
dotnet list EdgeGuard.Platform.slnx package --outdated
```

```bash
dotnet list EdgeGuard.Platform.slnx package --vulnerable --include-transitive
```
