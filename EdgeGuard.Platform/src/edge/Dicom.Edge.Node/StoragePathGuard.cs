using Microsoft.Data.Sqlite;

namespace Dicom.Edge.Node;

/// <summary>
/// Impide que la base SQLite quede por debajo de la raíz de almacenamiento DICOM.
///
/// <para>Cuando conviven, el recorrido que mide el workspace suma el archivo de base
/// como si fuera carga DICOM purgable. La limpieza de emergencia entonces borra
/// estudios para aliviar una presión que en parte no puede aliviar, porque SQLite no
/// devuelve páginas al sistema operativo sin un VACUUM.</para>
///
/// <para>La separación es una convención de despliegue, y las convenciones se erosionan:
/// basta un appsettings de cliente que apunte ambas rutas al mismo sitio. Esta
/// verificación la vuelve una invariante comprobada al arrancar.</para>
///
/// <para>El <c>dicomRootPath</c> recibido debe ser la raíz <i>efectiva</i> — la fila
/// <c>storage.root_path</c> ya resuelta a absoluta, ver <c>NodeStorageRoot</c> —, no una
/// clave de appsettings. Comprobar una ruta que nadie usa deja pasar el caso real.</para>
/// </summary>
internal static class StoragePathGuard
{
    /// <summary>
    /// Devuelve un mensaje de error si la base vive dentro de la raíz DICOM, o
    /// <c>null</c> si las rutas están correctamente separadas.
    /// </summary>
    internal static string? Validate(string sqliteConnectionString, string dicomRootPath)
    {
        if (DatabaseFile(sqliteConnectionString) is not { } dbFull)
            return null;
        if (string.IsNullOrWhiteSpace(dicomRootPath))
            return null;

        var rootFull = Path.GetFullPath(dicomRootPath);

        // El separador final evita que "/data-backup" cuente como dentro de "/data".
        var rootPrefix = rootFull.EndsWith(Path.DirectorySeparatorChar)
            ? rootFull
            : rootFull + Path.DirectorySeparatorChar;

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!dbFull.StartsWith(rootPrefix, comparison))
            return null;

        return $"La base de datos del nodo ({dbFull}) está dentro de la raíz de almacenamiento " +
               $"DICOM ({rootFull}). Deben ser directorios separados: de lo contrario la medición " +
               "del workspace cuenta la base como carga DICOM y la limpieza de emergencia borra " +
               "estudios para aliviar una presión que no puede aliviar. Mueva la base fuera de " +
               "la raíz — la convención de despliegue es una carpeta 'persistence' hermana de 'data'.";
    }

    /// <summary>
    /// Ruta absoluta del archivo SQLite, o <c>null</c> si la cadena de conexión es inválida
    /// o la base es en memoria (no toca el disco, así que no puede solaparse con nada).
    /// </summary>
    /// <remarks>
    /// Se resuelve contra el directorio de trabajo porque es así como lo hace SQLite con un
    /// <c>Data Source</c> relativo. Por eso en producción la cadena debe ser absoluta.
    /// </remarks>
    internal static string? DatabaseFile(string sqliteConnectionString)
    {
        string dbPath;
        try
        {
            dbPath = new SqliteConnectionStringBuilder(sqliteConnectionString).DataSource;
        }
        catch (ArgumentException)
        {
            // Una cadena de conexión inválida es problema de otra capa, no de esta.
            return null;
        }

        if (string.IsNullOrWhiteSpace(dbPath) || dbPath.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
            return null;

        return Path.GetFullPath(dbPath);
    }
}
