# Testing Strategy

EdgeGuard Platform uses a tiered testing approach: fast unit tests form the bulk of coverage, integration tests validate data persistence and protocol boundaries, and end-to-end tests cover critical user paths in the SPA.

---

## Table of Contents

1. [Testing Pyramid](#testing-pyramid)
2. [Unit Tests](#unit-tests)
   - [Domain Aggregates](#domain-aggregates)
   - [HL7 Message Parser](#hl7-message-parser)
   - [Validation Service](#validation-service)
3. [Integration Tests](#integration-tests)
   - [EF Core Repositories](#ef-core-repositories)
   - [HL7 Pipeline End-to-End](#hl7-pipeline-end-to-end)
4. [End-to-End Tests](#end-to-end-tests)
5. [Test Data](#test-data)
6. [CI Guidance](#ci-guidance)
7. [Coverage Targets](#coverage-targets)

---

## Testing Pyramid

| Layer | Target share | Characteristics |
|-------|-------------|-----------------|
| Unit | ~80% | In-memory, no I/O, milliseconds per test |
| Integration | ~15% | Real PostgreSQL via TestContainers, real HL7 TCP |
| E2E | ~5% | Browser-based, Playwright, full stack |

The pyramid ensures the majority of regressions are caught by fast-running unit tests, reserving expensive infrastructure-dependent tests for boundary validation.

---

## Unit Tests

Unit tests are located in `src/tests/unit/`. They run without any external dependencies.

### Domain Aggregates

Domain aggregate tests verify state machine correctness, invariant enforcement, and domain event emission.

#### Study state transitions

```csharp
[Fact]
public void TransitionStatus_FromReceived_ToSending_Succeeds()
{
    // Arrange
    var study = Study.Create(new DicomUid("1.2.3.4"), PatientId.New());
    study.TransitionStatus(StudyStatus.Received);

    // Act
    study.TransitionStatus(StudyStatus.Sending);

    // Assert
    study.Status.Should().Be(StudyStatus.Sending);
    study.StatusHistory.Should().HaveCount(2);
    study.DomainEvents.Should().ContainSingle(e => e is StudyStatusChangedEvent);
}

[Fact]
public void TransitionStatus_FromSent_ToAnyStatus_Throws()
{
    // Arrange
    var study = Study.Create(new DicomUid("1.2.3.4"), PatientId.New());
    study.TransitionStatus(StudyStatus.Received);
    study.TransitionStatus(StudyStatus.Sending);
    study.TransitionStatus(StudyStatus.Sent);

    // Act & Assert
    var act = () => study.TransitionStatus(StudyStatus.Sending);
    act.Should().Throw<InvalidOperationException>()
        .WithMessage("*Sent*terminal*");
}

[Fact]
public void TransitionStatus_FromFailed_ToSending_Succeeds_AsRetry()
{
    var study = Study.Create(new DicomUid("1.2.3.4"), PatientId.New());
    study.TransitionStatus(StudyStatus.Received);
    study.TransitionStatus(StudyStatus.Sending);
    study.TransitionStatus(StudyStatus.Failed, "PACS unreachable");

    // Retry is the only valid transition from Failed
    study.TransitionStatus(StudyStatus.Sending);
    study.Status.Should().Be(StudyStatus.Sending);
}
```

#### Patient merge invariants

```csharp
[Fact]
public void Patient_MergeIntoSelf_Throws()
{
    var patient = Patient.Create(new PatientIdentifier("MRN001"), "Smith, John");
    var act = () => patient.MergeInto(patient);
    act.Should().Throw<InvalidOperationException>();
}

[Fact]
public void Patient_MergeIntoDeletedPatient_Throws()
{
    var source = Patient.Create(new PatientIdentifier("MRN001"), "Smith, John");
    var deleted = Patient.Create(new PatientIdentifier("MRN002"), "Doe, Jane");
    deleted.MergeInto(Patient.Create(new PatientIdentifier("MRN003"), "Target"));

    var act = () => source.MergeInto(deleted);
    act.Should().Throw<InvalidOperationException>();
}
```

#### Value object validation

```csharp
[Theory]
[InlineData("1.2.840.10008.5.1.4.1.1.2")]  // Valid CT Storage SOP
[InlineData("1.2.3.4")]                       // Valid minimal UID
public void DicomUid_ValidFormats_Create_Successfully(string uid)
{
    var act = () => new DicomUid(uid);
    act.Should().NotThrow();
}

[Theory]
[InlineData("")]
[InlineData("not.a.valid.uid.because.it.has.letters")]
[InlineData("1.2.3.")] // trailing dot
public void DicomUid_InvalidFormats_Throw(string uid)
{
    var act = () => new DicomUid(uid);
    act.Should().Throw<ArgumentException>();
}
```

### HL7 Message Parser

HL7 parser tests use sample message files stored in `src/tests/TestData/hl7/`.

```csharp
public class Hl7MessageParserTests
{
    private readonly Hl7MessageParser _parser = new();

    [Fact]
    public void Parse_AdtA01_ExtractsPatientId()
    {
        var raw = File.ReadAllText("TestData/hl7/adt_a01_sample.hl7");
        var result = _parser.Parse(raw);

        result.IsSuccess.Should().BeTrue();
        result.Value.MessageType.Should().Be("ADT^A01");
        result.Value.PatientId.Should().Be("MRN123456");
        result.Value.PatientName.Should().Be("Smith^John^A");
    }

    [Fact]
    public void Parse_AdtA40_ExtractsMrgSegment()
    {
        var raw = File.ReadAllText("TestData/hl7/adt_a40_merge.hl7");
        var result = _parser.Parse(raw);

        result.IsSuccess.Should().BeTrue();
        result.Value.MessageType.Should().Be("ADT^A40");
        result.Value.PatientId.Should().Be("NEW_MRN789");
        result.Value.PriorPatientId.Should().Be("OLD_MRN123");
    }

    [Fact]
    public void Parse_OrmO01_ExtractsProcedureDetails()
    {
        var raw = File.ReadAllText("TestData/hl7/orm_o01_order.hl7");
        var result = _parser.Parse(raw);

        result.Value.ProcedureCode.Should().Be("71046");
        result.Value.ScheduledDate.Should().Be(new DateOnly(2025, 1, 15));
        result.Value.AccessionNumber.Should().Be("ACC20250115002");
    }

    [Fact]
    public void Parse_OruR01_ExtractsObxImageLink()
    {
        var raw = File.ReadAllText("TestData/hl7/oru_r01_with_image.hl7");
        var result = _parser.Parse(raw);

        result.Value.ObservationValueType.Should().Be("RP");
        result.Value.ImageUrl.Should().Be("https://pacs.example.com/viewer/1.2.3.4");
    }
}
```

### Validation Service

Validation service tests cover all supported message types plus known error cases.

```csharp
public class Hl7MessageValidationServiceTests
{
    private readonly Hl7MessageValidationService _sut = new();

    [Theory]
    [InlineData("ADT^A01")]
    [InlineData("ADT^A40")]
    [InlineData("ORM^O01")]
    [InlineData("ORU^R01")]
    public void Validate_SupportedMessageTypes_ReturnsValid(string messageType)
    {
        var message = CreateMinimalValidMessage(messageType);
        var result = _sut.Validate(message);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AdtA40_MissingMrg_ReturnsError()
    {
        var message = CreateAdtA40WithoutMrg();
        var result = _sut.Validate(message);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Field == "MRG-1");
    }

    [Fact]
    public void Validate_UnsupportedTriggerEvent_ReturnsError()
    {
        var message = CreateMessageWithType("ADT^A08"); // Update not supported
        var result = _sut.Validate(message);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "UNSUPPORTED_TRIGGER_EVENT");
    }

    [Fact]
    public void Validate_MissingPid3_ReturnsError()
    {
        var message = CreateAdtA01WithoutPid3();
        var result = _sut.Validate(message);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Field == "PID-3");
    }
}
```

---

## Integration Tests

Integration tests are in `src/tests/integration/`. They require Docker for TestContainers.

### EF Core Repositories

Tests run against a real PostgreSQL instance spun up by TestContainers. Each test class uses a fresh database.

```csharp
public class StudyRepositoryTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgres = null!;
    private EdgeGuardDbContext _context = null!;
    private StudyRepository _sut = null!;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<EdgeGuardDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new EdgeGuardDbContext(options);
        await _context.Database.MigrateAsync();
        _sut = new StudyRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ThenGetByUid_ReturnsStudy()
    {
        var uid = new DicomUid("1.2.3.4.5");
        var patient = Patient.Create(new PatientIdentifier("MRN001"), "Smith, John");
        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        var study = Study.Create(uid, patient.Id);
        await _sut.AddAsync(study);
        await _context.SaveChangesAsync();

        var retrieved = await _sut.GetByUidAsync(uid);

        retrieved.Should().NotBeNull();
        retrieved!.StudyInstanceUid.Should().Be(uid);
    }

    [Fact]
    public async Task AddAsync_DuplicateUid_ThrowsDbException()
    {
        var uid = new DicomUid("1.2.3.4.5");
        var patient = Patient.Create(new PatientIdentifier("MRN002"), "Doe, Jane");
        await _context.Patients.AddAsync(patient);
        var study1 = Study.Create(uid, patient.Id);
        var study2 = Study.Create(uid, patient.Id);

        await _sut.AddAsync(study1);
        await _context.SaveChangesAsync();

        var act = async () =>
        {
            await _sut.AddAsync(study2);
            await _context.SaveChangesAsync();
        };

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
```

### HL7 Pipeline End-to-End

Tests the complete HL7 processing pipeline by sending a real MLLP message over TCP and verifying the database state.

```csharp
[Collection("IntegrationTests")]
public class Hl7PipelineIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;

    public Hl7PipelineIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SendAdtA01_OverMllp_CreatesPatientInDb()
    {
        // Arrange
        var message = File.ReadAllText("TestData/hl7/adt_a01_sample.hl7");

        // Act
        var ack = await _fixture.MllpClient.SendAsync(message, port: 8001);

        // Assert ACK
        ack.Should().Contain("MSA|AA|");

        // Assert database state
        await _fixture.WaitForConditionAsync(
            async () => await _fixture.PatientRepository.GetByIdentifierAsync(
                new PatientIdentifier("MRN123456")) is not null,
            timeout: TimeSpan.FromSeconds(5));

        var patient = await _fixture.PatientRepository.GetByIdentifierAsync(
            new PatientIdentifier("MRN123456"));
        patient.Should().NotBeNull();
        patient!.FullName.Should().Be("Smith, John A");
    }

    [Fact]
    public async Task SendOrmO01_OverMllp_CreatesScheduledStudy()
    {
        var message = File.ReadAllText("TestData/hl7/orm_o01_order.hl7");
        var ack = await _fixture.MllpClient.SendAsync(message, port: 8001);

        ack.Should().Contain("MSA|AA|");

        await _fixture.WaitForConditionAsync(
            async () => (await _fixture.StudyRepository.GetByStatusAsync(StudyStatus.Scheduled))
                .Any(s => s.AccessionNumber == "ACC20250115002"),
            timeout: TimeSpan.FromSeconds(5));
    }
}
```

---

## End-to-End Tests

E2E tests use Playwright for Angular and are located in `src/frontend/dicomedge-ui/e2e/`.

### Critical paths covered

| Test | Description |
|------|-------------|
| Login flow | User can log in with valid credentials; invalid credentials show error |
| Study list | Studies page loads, displays list, supports search and filter |
| Study detail | Clicking a study opens detail view with status and series information |
| Create routing rule | Admin can navigate to Settings, create a routing rule, and see it in the list |
| Node status | Dashboard shows node connectivity status, updates in real time |

### Example Playwright test

```typescript
// e2e/studies.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Studies page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.fill('[data-testid="username"]', 'testadmin');
    await page.fill('[data-testid="password"]', 'TestPassword123!');
    await page.click('[data-testid="login-button"]');
    await expect(page).toHaveURL('/studies');
  });

  test('displays study list after login', async ({ page }) => {
    await expect(page.locator('[data-testid="study-list"]')).toBeVisible();
    await expect(page.locator('[data-testid="study-row"]')).toHaveCount(
      expect.any(Number)
    );
  });

  test('can search studies by accession number', async ({ page }) => {
    await page.fill('[data-testid="study-search"]', 'ACC20250115');
    await expect(page.locator('[data-testid="study-row"]')).toHaveCount(1);
    await expect(page.locator('[data-testid="study-row"]'))
      .toContainText('ACC20250115');
  });

  test('navigate to study detail', async ({ page }) => {
    await page.click('[data-testid="study-row"]:first-child');
    await expect(page.locator('[data-testid="study-detail"]')).toBeVisible();
    await expect(page.locator('[data-testid="study-status"]')).toBeVisible();
  });
});
```

---

## Test Data

### HL7 sample messages

Located in `src/tests/TestData/hl7/`:

| File | Message Type | Description |
|------|-------------|-------------|
| `adt_a01_sample.hl7` | ADT^A01 | Patient registration with all fields |
| `adt_a01_minimal.hl7` | ADT^A01 | Minimal required fields only |
| `adt_a40_merge.hl7` | ADT^A40 | Patient merge with MRG segment |
| `adt_a40_missing_mrg.hl7` | ADT^A40 | Invalid — missing MRG, for negative tests |
| `orm_o01_order.hl7` | ORM^O01 | Standard imaging order |
| `orm_o01_with_study_date.hl7` | ORM^O01 | Order with explicit study date |
| `oru_r01_with_image.hl7` | ORU^R01 | Result with OBX RP image link |
| `oru_r01_text_only.hl7` | ORU^R01 | Result with text report, no image link |

### DICOM sample files

Located in `src/tests/TestData/dicom/`:

| File | Modality | Description |
|------|----------|-------------|
| `ct_single_instance.dcm` | CT | Single CT slice for basic C-STORE tests |
| `mr_series_5_instances.dcm` | MR | 5-instance MR series for series grouping tests |
| `cr_image.dcm` | CR | Computed radiography image |

> **PHI note:** All test DICOM files use synthetic patient data generated for testing. They contain no real patient information.

---

## CI Guidance

### Unit and integration tests — dotnet test

```yaml
# .github/workflows/ci.yml (example)
jobs:
  test-backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore --configuration Release
      - name: Unit Tests
        run: dotnet test --no-build --configuration Release --filter "Category=Unit"
      - name: Integration Tests
        run: dotnet test --no-build --configuration Release --filter "Category=Integration"
        # Note: TestContainers requires Docker, available on ubuntu-latest runners
```

### Playwright E2E — separate stage

E2E tests require a running instance of the full stack and are run in a dedicated CI stage after deployment to a staging environment:

```yaml
  test-e2e:
    runs-on: ubuntu-latest
    needs: [deploy-staging]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
      - name: Install dependencies
        run: npm ci
        working-directory: src/frontend/dicomedge-ui
      - name: Install Playwright browsers
        run: npx playwright install --with-deps
        working-directory: src/frontend/dicomedge-ui
      - name: Run E2E tests
        run: npx playwright test
        working-directory: src/frontend/dicomedge-ui
        env:
          BASE_URL: ${{ vars.STAGING_URL }}
          TEST_USERNAME: ${{ secrets.E2E_TEST_USERNAME }}
          TEST_PASSWORD: ${{ secrets.E2E_TEST_PASSWORD }}
      - name: Upload test artifacts
        uses: actions/upload-artifact@v4
        if: failure()
        with:
          name: playwright-report
          path: src/frontend/dicomedge-ui/playwright-report/
```

---

## Coverage Targets

| Project | Coverage target | Enforcement |
|---------|----------------|-------------|
| `Dicom.Edge.Hub.Domain` | 90% line | CI gate |
| `Dicom.Edge.Hub.Application` | 80% line | CI gate |
| `Dicom.Edge.Hub.Persistence` | Covered by integration tests | N/A |
| `Dicom.Edge.Node` | 75% line | Tracked |
| Angular SPA | E2E coverage on critical paths | Playwright |

Coverage reports are generated with `dotnet test --collect:"XPlat Code Coverage"` and uploaded to Codecov (or equivalent) in CI.
