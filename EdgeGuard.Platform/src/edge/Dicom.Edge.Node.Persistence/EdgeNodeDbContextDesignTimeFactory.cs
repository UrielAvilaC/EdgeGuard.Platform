using Dicom.Edge.Node.Persistence.Context;
using Dicom.Edge.Node.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dicom.Edge.Node.Persistence;

/// <summary>
/// Design-time factory for <see cref="EdgeNodeDbContext"/>.
/// Used by EF Core tools (Add-Migration, Update-Database) so they do not
/// need to build the full application host and its DI container.
/// </summary>
internal sealed class EdgeNodeDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<EdgeNodeDbContext>
{
    public EdgeNodeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<EdgeNodeDbContext>()
            .UseSqlite(
                "Data Source=edge-node-design.db",
                sql => sql.MigrationsAssembly(typeof(EdgeNodeDbContext).Assembly.FullName))
            .AddInterceptors(new TimestampInterceptor())
            .Options;

        return new EdgeNodeDbContext(options);
    }
}
