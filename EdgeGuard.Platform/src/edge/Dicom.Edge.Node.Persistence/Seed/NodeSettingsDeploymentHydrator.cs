using Dicom.Edge.Abstractions.Configuration;
using Dicom.Edge.Node.Persistence.Configuration;
using Microsoft.Extensions.Configuration;

namespace Dicom.Edge.Node.Persistence.Seed;

/// <summary>
/// Copia la configuración de despliegue —la que el instalador escribe en appsettings— a
/// las filas de <c>node_settings</c> que el runtime lee de verdad.
///
/// <para>Hermano de <see cref="NodeSettingsHubHydrator"/> y con la misma regla, aplicada a
/// otras cuatro claves: identidad DICOM, puertos y raíz de almacenamiento. Existe porque
/// esas cuatro NO se pueden configurar por archivo — el proveedor de configuración de la
/// base se registra después de appsettings, así que su fila gana siempre. Sin este paso,
/// un nodo recién instalado arrancaría con el AE, los puertos y la carpeta de fábrica sin
/// importar lo que dijera su configuración, y habría que ajustarlo a mano desde el Hub:
/// un despliegue a dos tiempos, que es justo lo que un instalador debe evitar.</para>
///
/// <para><b>Precedencia.</b> Mientras el nodo no se haya registrado (no tiene
/// <c>hub.api_key</c>) no está configurado todavía, así que manda el despliegue y las filas
/// se reescriben en cada arranque. Una vez registrado manda la base, y solo se rellenan las
/// filas vacías: así sobreviven las ediciones que el operador haga desde el Hub.</para>
/// </summary>
internal static class NodeSettingsDeploymentHydrator
{
    /// <summary>Longitud máxima de un AE title por el estándar DICOM.</summary>
    private const int MaxAeTitleLength = 16;

    /// <summary>
    /// Escribe en <c>node_settings</c> los valores desplegados de identidad DICOM, puertos
    /// y raíz de almacenamiento.
    /// </summary>
    /// <returns>Número de filas escritas.</returns>
    /// <exception cref="InvalidOperationException">
    /// El AE title desplegado no cumple el estándar DICOM. Se aborta el arranque en lugar
    /// de escribirlo: un AE inválido no produce un error claro, produce asociaciones
    /// rechazadas por el PACS y por las modalidades, cada una con su propio mensaje.
    /// </exception>
    public static async Task<int> HydrateFromConfigurationAsync(
        EdgeNodeDbContext ctx,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        var values = BuildFromConfiguration(configuration);

        if (values.Count == 0)
        {
            logger.LogDebug("Deployment settings hydration skipped — nothing deployed to apply");
            return 0;
        }

        var keys = values.Keys.ToList();
        var rows = await ctx.NodeSettings
            .Where(s => keys.Contains(s.Key))
            .ToListAsync(ct);

        // Mismo criterio que el hidratador de Hub: sin API key el nodo nunca completó
        // registro, luego no está configurado y decide el despliegue.
        var apiKey = await ctx.NodeSettings
            .Where(s => s.Key == NodeSettingKeys.Hub.ApiKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
        var registered = !string.IsNullOrWhiteSpace(apiKey);

        var written = new List<string>();

        foreach (var row in rows)
        {
            if (registered && !string.IsNullOrWhiteSpace(row.Value)) continue;
            if (!values.TryGetValue(row.Key, out var value)) continue;
            if (string.Equals(row.Value, value, StringComparison.Ordinal)) continue;

            row.Value = value;
            row.UpdatedAt = DateTime.UtcNow;
            written.Add($"{row.Key}={value}");
        }

        if (written.Count == 0)
        {
            logger.LogDebug(
                "Deployment settings hydration: nothing to do (registered={Registered})", registered);
            return 0;
        }

        await ctx.SaveChangesAsync(ct);

        logger.LogInformation(
            registered
                ? "Deployment settings hydrated from appsettings — {Count} empty row(s) filled: [{Settings}]"
                : "Node is not registered — {Count} deployment row(s) reset from appsettings: [{Settings}]",
            written.Count, string.Join(", ", written));

        return written.Count;
    }

    // ── appsettings → DB key map ─────────────────────────────────────────────

    /// <summary>
    /// Proyecta la configuración desplegada sobre las claves que el despliegue posee.
    /// Las ausentes se omiten, de modo que la fila conserva lo que tuviera.
    /// </summary>
    private static Dictionary<string, string> BuildFromConfiguration(IConfiguration configuration)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // El AE title es, en la práctica, la identidad del nodo: con él se anuncia al
        // Hub, con él lo llaman las modalidades y con él sale como SCU hacia el PACS.
        // Los tres se derivan de esta única fila — ver NodeAeTitle y DeriveNodeAeTitle —,
        // así que escribirla mal desalinea las tres a la vez.
        var aeTitle = Deployed(configuration, NodeAeTitle.ConfigPath);
        if (!string.IsNullOrWhiteSpace(aeTitle))
        {
            Add(map, NodeSettingKeys.Dicom.AeTitle, ValidateAeTitle(aeTitle));
        }

        Add(map, NodeSettingKeys.Dicom.Port,     ValidatePort(configuration, ConfigPaths.DicomPort,  "el puerto DICOM"));
        Add(map, NodeSettingKeys.NodeApi.Port,   ValidatePort(configuration, ConfigPaths.NodeApiPort, "el puerto de la API del nodo"));
        Add(map, NodeSettingKeys.Storage.RootPath, Deployed(configuration, ConfigPaths.StorageRootPath));

        return map;
    }

    /// <summary>
    /// Comprueba el AE contra el estándar DICOM: hasta 16 caracteres, sin espacios y sin
    /// caracteres de control. Devuelve el valor recortado.
    /// </summary>
    private static string ValidateAeTitle(string aeTitle)
    {
        var trimmed = aeTitle.Trim();

        if (trimmed.Length > MaxAeTitleLength)
            throw new InvalidOperationException(
                $"El AE title desplegado ('{trimmed}') tiene {trimmed.Length} caracteres y el " +
                $"estándar DICOM admite {MaxAeTitleLength}. Corrígelo en " +
                $"la clave '{NodeAeTitle.ConfigPath}' de la configuración desplegada.");

        if (trimmed.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
            throw new InvalidOperationException(
                $"El AE title desplegado ('{trimmed}') contiene espacios o caracteres de " +
                $"control, que el estándar DICOM no admite. Corrígelo en " +
                $"la clave '{NodeAeTitle.ConfigPath}' de la configuración desplegada.");

        return trimmed;
    }

    /// <summary>
    /// Devuelve el puerto desplegado, o <c>null</c> si no hay ninguno. Un valor presente
    /// pero inválido aborta el arranque en lugar de omitirse: dejar que el nodo escuche en
    /// el puerto de fábrica cuando el despliegue pidió otro es el fallo que nadie nota
    /// hasta que una modalidad no consigue enviar.
    /// </summary>
    private static string? ValidatePort(IConfiguration configuration, string key, string descripcion)
    {
        var raw = Deployed(configuration, key);
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port) &&
            port is >= 1 and <= 65535)
        {
            return port.ToString(CultureInfo.InvariantCulture);
        }

        throw new InvalidOperationException(
            $"El valor desplegado para {descripcion} ('{raw}', clave '{key}') no es un puerto " +
            "válido. Debe ser un entero entre 1 y 65535.");
    }

    /// <summary>
    /// Lee una clave de los proveedores desplegados (appsettings, variables de entorno…),
    /// saltando <see cref="NodeDatabaseConfigurationProvider"/>. Leer por
    /// <see cref="IConfiguration"/> devolvería las mismas filas que este hidratador está a
    /// punto de reescribir, porque la base tiene la precedencia más alta.
    /// </summary>
    private static string? Deployed(IConfiguration configuration, string key)
    {
        if (configuration is not IConfigurationRoot root)
            return configuration[key];

        foreach (var provider in root.Providers.Reverse())
        {
            if (provider is NodeDatabaseConfigurationProvider) continue;
            if (provider.TryGet(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static void Add(Dictionary<string, string> map, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            map[key] = value;
    }
}
