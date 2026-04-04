using Dicom.Edge.Hub.Domain.Aggregates.Cleanup;
using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Hub database.
/// </summary>
public class HubDbContext : DbContext
{
    public HubDbContext(DbContextOptions<HubDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Node> Nodes => Set<Node>();
    public DbSet<NodePacsAssignment> NodePacsAssignments => Set<NodePacsAssignment>();
    public DbSet<PacsServer> PacsServers => Set<PacsServer>();
    public DbSet<Study> Studies => Set<Study>();
    public DbSet<StudySeries> StudySeries => Set<StudySeries>();
    public DbSet<StudyStatusAudit> StudyStatusAudits => Set<StudyStatusAudit>();
    public DbSet<HealthCheckRecord> HealthCheckRecords => Set<HealthCheckRecord>();
    public DbSet<PacsCEchoResult> PacsCEchoResults => Set<PacsCEchoResult>();
    public DbSet<StudyCleanupPolicy> StudyCleanupPolicies => Set<StudyCleanupPolicy>();
    public DbSet<Hl7Message> Hl7Messages => Set<Hl7Message>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<Hl7RoutingRule> Hl7RoutingRules => Set<Hl7RoutingRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HubDbContext).Assembly);
    }
}
