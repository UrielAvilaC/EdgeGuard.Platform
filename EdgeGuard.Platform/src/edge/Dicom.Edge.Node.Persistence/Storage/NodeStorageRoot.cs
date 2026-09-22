using Microsoft.Data.Sqlite;

namespace Dicom.Edge.Node.Persistence.Storage;

/// <summary>
/// Resuelve la raíz de almacenamiento DICOM efectiva del nodo.
///
/// <para>El valor configurable vive en la fila <c>storage.root_path</c> de
/// <c>node_settings</c> — no en appsettings. Es la fila que lee
/// <see cref="Services.NodeSettingsService"/> y, por tanto, la que acaban usando el
/// proveedor de almacenamiento, la limpieza y la medición del workspace.</para>
///
/// <para>El valor sembrado es relativo (<c>./data</c>). Una ruta relativa se resolvía
/// contra el directorio de trabajo del proceso, que para un servicio de Windows es
/// <c>C:\Windows\System32</c>: el SCM no lo hereda del ejecutable. Aquí se ancla al
/// content root — el directorio del binario cuando corre como servicio, el del proyecto
/// en desarrollo — que es lo que la convención de despliegue asume.</para>
/// </summary>
public static class NodeStorageRoot
{
    /// <summary>Valor sembrado en <c>node_settings</c> para <c>storage.root_path</c>.</summary>
    public const string DefaultRootPath = "./data";

    /// <summary>
    /// Clave de la configuración desplegada que fija la raíz. No la lee el runtime: es la
    /// que <c>NodeSettingsDeploymentHydrator</c> copia a <c>storage.root_path</c> al
    /// arrancar, y por eso gana mientras el nodo no esté registrado.
    /// </summary>
    public const string DeployedConfigPath = Configuration.ConfigPaths.StorageRootPath;

    /// <summary>
    /// Convierte el valor configurado en una ruta absoluta anclada a
    /// <paramref name="contentRootPath"/>. Una ruta ya absoluta pasa intacta.
    /// </summary>
    public static string Resolve(string? configuredRootPath, string contentRootPath)
    {
        var root = string.IsNullOrWhiteSpace(configuredRootPath)
            ? DefaultRootPath
            : configuredRootPath.Trim();

        return string.IsNullOrWhiteSpace(contentRootPath)
            ? Path.GetFullPath(root)
            : Path.GetFullPath(root, contentRootPath);
    }

    /// <summary>
    /// Lee <c>storage.root_path</c> directamente de SQLite, sin EF ni contenedor, para
    /// poder verificar el layout antes de construir el host.
    /// </summary>
    /// <returns>
    /// El valor configurado, o <c>null</c> si la base todavía no existe (primer arranque,
    /// migraciones pendientes) o la fila está vacía. En ese caso rige
    /// <see cref="DefaultRootPath"/>, que es lo que sembrará el arranque.
    /// </returns>
    public static string? ReadConfigured(string sqliteConnectionString)
    {
        try
        {
            // ReadOnly y sin pooling a propósito. El modo por defecto crea el archivo al
            // abrir, así que una sonda de solo lectura dejaría una base vacía antes de las
            // migraciones — y la dejaría incluso cuando la verificación aborta el arranque.
            // El pooling, además, conservaría el handle abierto hasta después de migrar.
            var readOnly = new SqliteConnectionStringBuilder(sqliteConnectionString)
            {
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            };

            using var connection = new SqliteConnection(readOnly.ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT value FROM {TableNames.NodeSettings} WHERE key = $key LIMIT 1";
            cmd.Parameters.AddWithValue("$key", NodeSettingKeys.Storage.RootPath);

            var value = cmd.ExecuteScalar() as string;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (SqliteException)
        {
            // Base o tabla inexistentes: primer arranque. El default decide.
            return null;
        }
        catch (InvalidOperationException)
        {
            // Cadena de conexión inutilizable: es problema de otra capa.
            return null;
        }
        catch (ArgumentException)
        {
            // Cadena de conexión malformada: idem.
            return null;
        }
    }
}
