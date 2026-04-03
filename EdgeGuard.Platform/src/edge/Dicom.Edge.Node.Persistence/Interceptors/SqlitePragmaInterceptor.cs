using Microsoft.Data.Sqlite;

namespace Dicom.Edge.Node.Persistence.Interceptors;

/// <summary>
/// Applies all required SQLite pragmas each time a connection is opened.
/// Enables WAL mode, memory-mapped I/O, and performance tuning for millions of rows.
/// </summary>
/// <remarks>
/// <c>auto_vacuum</c> is set only once (requires no tables to exist or a VACUUM).
/// All other pragmas are safe to re-apply on every connection open.
/// </remarks>
public sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
{
    private static readonly string[] Pragmas =
    [
        "PRAGMA journal_mode  = WAL",
        "PRAGMA synchronous   = NORMAL",
        "PRAGMA cache_size    = -64000",   // 64 MB page cache
        "PRAGMA temp_store    = MEMORY",
        "PRAGMA busy_timeout  = 5000",     // 5 s before SQLITE_BUSY
        "PRAGMA mmap_size     = 268435456",// 256 MB memory-mapped I/O
        "PRAGMA foreign_keys  = ON",
    ];

    private static int _autoVacuumSet;

    public override void ConnectionOpened(
        System.Data.Common.DbConnection connection,
        ConnectionEndEventData eventData)
    {
        ApplyPragmas(connection);
    }

    public override Task ConnectionOpenedAsync(
        System.Data.Common.DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ApplyPragmas(connection);
        return Task.CompletedTask;
    }

    private static void ApplyPragmas(System.Data.Common.DbConnection connection)
    {
        if (connection is not SqliteConnection sqliteConnection)
            return;

        foreach (var pragma in Pragmas)
        {
            using var cmd = sqliteConnection.CreateCommand();
            cmd.CommandText = pragma;
            cmd.ExecuteNonQuery();
        }

        // auto_vacuum can only be changed on empty DB or with explicit VACUUM.
        // Set once per process lifetime to avoid unnecessary warnings.
        if (Interlocked.CompareExchange(ref _autoVacuumSet, 1, 0) == 0)
        {
            using var cmd = sqliteConnection.CreateCommand();
            cmd.CommandText = "PRAGMA auto_vacuum = INCREMENTAL";
            cmd.ExecuteNonQuery();
        }
    }
}
