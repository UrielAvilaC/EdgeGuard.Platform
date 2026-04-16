using System.Text;
using Dicom.Edge.Hub.Domain.Aggregates.Audit;
using Dicom.Edge.Hub.Domain.Aggregates.Cleanup;
using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Dicom.Edge.Hub.Domain.Aggregates.NodeConfig;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Context;

/// <summary>
/// EF Core DbContext for the Hub database.
/// Manages all Hub aggregate roots and entities with PostgreSQL as the backing store.
/// Applies snake_case naming convention for all database objects (columns, indexes, keys, FKs).
/// Table names are set explicitly in each <see cref="IEntityTypeConfiguration{TEntity}"/>.
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
    public DbSet<HubAuditLog> HubAuditLogs => Set<HubAuditLog>();
    public DbSet<WhatsAppNotification> WhatsAppNotifications => Set<WhatsAppNotification>();
    public DbSet<WhatsAppTemplate> WhatsAppTemplates => Set<WhatsAppTemplate>();
    public DbSet<WhatsAppTemplateVariable> WhatsAppTemplateVariables => Set<WhatsAppTemplateVariable>();
    public DbSet<WhatsAppAutoSendRule> WhatsAppAutoSendRules => Set<WhatsAppAutoSendRule>();
    public DbSet<PacsSendAudit> PacsSendAudits => Set<PacsSendAudit>();
    public DbSet<NodeConfigurationProfile> NodeConfigurationProfiles => Set<NodeConfigurationProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HubDbContext).Assembly);

        // Global query filters: soft-delete
        modelBuilder.Entity<Study>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<Node>().HasQueryFilter(n => !n.IsDeleted);
        modelBuilder.Entity<Patient>().HasQueryFilter(p => !p.IsDeleted);

        // PostgreSQL standard: apply snake_case naming convention
        // Table names are set explicitly in each IEntityTypeConfiguration.
        // Columns, indexes, keys, and FK constraints are converted automatically.
        ApplySnakeCaseNaming(modelBuilder);
    }

    /// <summary>
    /// Converts all column names, index names, primary key constraint names,
    /// and foreign key constraint names to PostgreSQL-standard snake_case.
    /// </summary>
    private static void ApplySnakeCaseNaming(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Column names
            foreach (var property in entity.GetProperties())
            {
                // Owned types share the owner's table and PK column.
                // Skip renaming PK properties on owned types to avoid
                // column name conflicts with the owner's PK.
                if (entity.IsOwned() && property.IsPrimaryKey())
                    continue;

                var columnName = property.GetColumnName();
                if (columnName is not null)
                    property.SetColumnName(ToSnakeCase(columnName));
            }

            // Primary / alternate key constraint names
            // Skip owned types — they share the owner's table and PK constraint.
            if (!entity.IsOwned())
            {
                foreach (var key in entity.GetKeys())
                {
                    var name = key.GetName();
                    if (name is not null)
                        key.SetName(ToSnakeCase(name));
                }
            }

            // Foreign key constraint names
            foreach (var fk in entity.GetForeignKeys())
            {
                var name = fk.GetConstraintName();
                if (name is not null)
                    fk.SetConstraintName(ToSnakeCase(name));
            }

            // Index names
            foreach (var index in entity.GetIndexes())
            {
                var name = index.GetDatabaseName();
                if (name is not null)
                    index.SetDatabaseName(ToSnakeCase(name));
            }
        }
    }

    /// <summary>
    /// Converts a PascalCase or camelCase identifier to snake_case.
    /// Handles consecutive uppercase letters (abbreviations) correctly:
    /// <c>CEchoInterval</c> → <c>c_echo_interval</c>,
    /// <c>MaxStorageMb</c> → <c>max_storage_mb</c>,
    /// <c>Hl7Version</c> → <c>hl7_version</c>.
    /// </summary>
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder(input.Length + 10);

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if (char.IsUpper(c))
            {
                if (i > 0 && input[i - 1] != '_')
                {
                    // Insert underscore before uppercase letter when:
                    // 1. Previous char is lowercase/digit: "studyDate" → "study_date"
                    // 2. Previous char is uppercase AND next char is lowercase: "CEcho" → "c_echo"
                    var prevIsLowerOrDigit = char.IsLower(input[i - 1]) || char.IsDigit(input[i - 1]);
                    var isAbbreviationEnd = char.IsUpper(input[i - 1])
                                            && i + 1 < input.Length
                                            && char.IsLower(input[i + 1]);

                    if (prevIsLowerOrDigit || isAbbreviationEnd)
                        sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
