using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Tests;

/// <summary>
/// Arnés de base de datos real para las pruebas de persistencia.
///
/// <para>SQLite en memoria, no el proveedor InMemory de EF. La distinción es la prueba
/// entera: InMemory no implementa tokens de concurrencia —no emite <c>UPDATE</c>, no
/// cuenta filas afectadas y nunca lanza <see cref="DbUpdateConcurrencyException"/>—, así
/// que sobre él los casos de este proyecto pasarían en verde con el defecto intacto.
/// SQLite sí construye el <c>WHERE</c> con el valor original del token y sí verifica
/// cuántas filas tocó, que es exactamente el comportamiento que hay que fijar.</para>
///
/// <para>La conexión se mantiene abierta durante toda la vida del fixture porque una base
/// SQLite <c>:memory:</c> desaparece cuando se cierra su última conexión.</para>
/// </summary>
public sealed class HubSqliteFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<HubDbContext> _options;

    public HubSqliteFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<HubDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new HubDbContext(_options);
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// Un <see cref="HubDbContext"/> nuevo sobre la misma base.
    ///
    /// <para>Cada prueba usa contextos distintos para sembrar, para ejercitar y para
    /// verificar. No es ceremonia: verificar sobre el mismo contexto que ejecutó la
    /// operación consulta el rastreador de cambios, no la base, y por lo tanto daría
    /// verde aunque el <c>UPDATE</c> no hubiera afectado ninguna fila — que es
    /// precisamente el defecto bajo prueba.</para>
    /// </summary>
    public HubDbContext NewContext() => new(_options);

    public void Dispose()
    {
        _connection.Dispose();
    }
}
