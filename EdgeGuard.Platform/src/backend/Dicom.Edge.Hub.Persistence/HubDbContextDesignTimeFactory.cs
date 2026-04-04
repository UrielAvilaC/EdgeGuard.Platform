using Dicom.Edge.Hub.Persistence.Context;
using Dicom.Edge.Hub.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dicom.Edge.Hub.Persistence;

/// <summary>
/// Design-time factory for <see cref="HubDbContext"/>.
/// Used by EF Core tools (Add-Migration, Update-Database) so they do not
/// need to build the full application host and its DI container.
/// </summary>
internal sealed class HubDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<HubDbContext>
{
    public HubDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HubDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=hub_design;Username=postgres;Password=postgres",
                sql => sql.MigrationsAssembly(typeof(HubDbContext).Assembly.FullName))
            .AddInterceptors(new TimestampInterceptor())
            .Options;

        return new HubDbContext(options);
    }
}
