using Dicom.Edge.Diagnostics.Configuration;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Diagnostics.HealthChecks;

/// <summary>
/// Indica qué carpeta vigila <see cref="StorageHealthCheck"/>.
///
/// <para>El chequeo mide el espacio libre de la unidad que contiene esa carpeta, así que
/// tiene que ser la carpeta donde el servicio realmente escribe. Cuando la ruta efectiva
/// no vive en la configuración desplegada — en el nodo la decide la fila
/// <c>storage.root_path</c>, editable desde el Hub sin reiniciar — una ruta fija en
/// appsettings acaba vigilando un disco que no es el que se llena.</para>
/// </summary>
public interface IStorageHealthLocation
{
    /// <summary>
    /// Carpeta a vigilar, o <c>null</c> si no se puede determinar ahora mismo; en ese caso
    /// el chequeo cae a la ruta configurada. Se consulta en cada chequeo a propósito: la
    /// ruta puede cambiar en caliente.
    /// </summary>
    ValueTask<string?> GetPathAsync(CancellationToken ct = default);
}

/// <summary>
/// Implementación por defecto: la ruta fija de <c>Diagnostics:HealthChecks:StoragePath</c>.
/// Sirve para los servicios cuya carpeta de trabajo sí es una decisión de despliegue.
/// </summary>
public sealed class ConfiguredStorageHealthLocation(
    IOptions<HealthCheckThresholdOptions> healthOptions) : IStorageHealthLocation
{
    public ValueTask<string?> GetPathAsync(CancellationToken ct = default)
        => ValueTask.FromResult(healthOptions.Value.StoragePath);
}
