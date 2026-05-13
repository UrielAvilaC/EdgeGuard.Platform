using Dicom.Edge.Node.Persistence.Interceptors;

namespace Dicom.Edge.Node.Persistence.Context;

/// <summary>
/// EF Core DbContext for the Edge Node SQLite database.
/// All table names use snake_case (configured in EntityConfigurations).
/// Interceptors are registered via AddDbContextFactory in PersistenceExtensions.
/// </summary>
public sealed class EdgeNodeDbContext(DbContextOptions<EdgeNodeDbContext> options)
    : DbContext(options)
{
    // ── Config & Registration ─────────────────────────────────────────────────
    public DbSet<NodeSettingEntity>      NodeSettings           { get; init; }
    public DbSet<NodeRegistrationEntity> NodeRegistration       { get; init; }
    public DbSet<ModalityConfiguration>  ModalityConfigurations { get; init; }

    // ── DICOM Core ────────────────────────────────────────────────────────────
    public DbSet<DicomPatient>  Patients  { get; init; }
    public DbSet<DicomStudy>    Studies   { get; init; }
    public DbSet<DicomSeries>   Series    { get; init; }
    public DbSet<DicomInstance> Instances { get; init; }

    // ── Queue & Transfer ──────────────────────────────────────────────────────
    public DbSet<EdgeQueueItem>  QueueItems { get; init; }
    public DbSet<StudyTransfer>  Transfers  { get; init; }
    public DbSet<TransferError>  Errors     { get; init; }

    // ── Audit & Associations ──────────────────────────────────────────────────
    public DbSet<AuditLog>         AuditLogs    { get; init; }
    public DbSet<DicomAssociation> Associations { get; init; }

    // ── Archive & Metrics ─────────────────────────────────────────────────────
    public DbSet<StudyArchive>  Archives { get; init; }
    public DbSet<StudyMetrics>  Metrics  { get; init; }

    // ── Routing & Worklist ────────────────────────────────────────────────────
    public DbSet<RoutingRule>    RoutingRules    { get; init; }
    public DbSet<WorklistItem>   WorklistItems   { get; init; }

    // ── PACS Servers (synced from Hub assignments) ────────────────────────────
    public DbSet<NodePacsServer> NodePacsServers { get; init; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(EdgeNodeDbContext).Assembly);

        // Global query filter: soft-deleted studies are excluded by default.
        // Use .IgnoreQueryFilters() when cleanup needs to see deleted rows.
        mb.Entity<DicomStudy>().HasQueryFilter(s => !s.IsDeleted);

        base.OnModelCreating(mb);
    }
}
