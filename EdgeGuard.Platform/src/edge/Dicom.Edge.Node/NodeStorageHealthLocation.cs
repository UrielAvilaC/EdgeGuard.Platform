using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Diagnostics.HealthChecks;

namespace Dicom.Edge.Node;

/// <summary>
/// Apunta el chequeo de almacenamiento a la raíz DICOM efectiva del nodo.
///
/// <para>La raíz la decide la fila <c>storage.root_path</c>, que el Hub puede cambiar en
/// caliente; <c>Diagnostics:HealthChecks:StoragePath</c> era una segunda ruta, fija en
/// appsettings, que podía quedar en otra unidad. El chequeo vigilaba entonces el espacio
/// libre de un disco que no era el que se llenaba — y avisaba tarde o no avisaba.</para>
/// </summary>
internal sealed class NodeStorageHealthLocation(INodeSettingsService settings) : IStorageHealthLocation
{
    public async ValueTask<string?> GetPathAsync(CancellationToken ct = default)
    {
        // Antes de las migraciones la consulta falla; devolver null deja que el chequeo
        // use la ruta configurada en lugar de reportar un fallo de almacenamiento.
        try
        {
            var storage = await settings.GetStorageConfigAsync(ct);
            return storage.RootPath;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }
}
