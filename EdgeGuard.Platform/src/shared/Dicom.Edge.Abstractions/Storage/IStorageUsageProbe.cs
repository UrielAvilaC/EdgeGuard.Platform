namespace Dicom.Edge.Abstractions.Storage;

/// <summary>
/// Fotografía del consumo de almacenamiento de un nodo en un instante dado.
///
/// <para>Son cuatro magnitudes y no una porque responden preguntas distintas: la
/// cuota dice si el nodo se está pasando de lo que se le asignó, y el volumen
/// dice si la máquina se va a quedar sin disco por causas que nada tienen que
/// ver con el workspace — logs, actualizaciones, otro servicio.</para>
/// </summary>
/// <param name="DicomMb">Peso de los estudios vivos.</param>
/// <param name="DatabaseMb">Peso de la base del nodo con sus archivos -wal y -shm.</param>
/// <param name="VolumeFreeMb">Libre del volumen; null si no se pudo consultar.</param>
/// <param name="VolumeTotalMb">Total del volumen; null si no se pudo consultar.</param>
/// <param name="LimitMb">Cuota vigente en el nodo. 0 = sin límite.</param>
/// <param name="MeasuredAt">Momento de la medición, que no es el del envío.</param>
public sealed record StorageUsage(
    long DicomMb,
    long DatabaseMb,
    long? VolumeFreeMb,
    long? VolumeTotalMb,
    long LimitMb,
    DateTime MeasuredAt)
{
    /// <summary>Lo que se compara contra la cuota.</summary>
    public long TotalUsedMb => DicomMb + DatabaseMb;
}

/// <summary>
/// Mide cuánto ocupa el nodo. La medición de rutina sale de contadores que el
/// nodo ya mantiene, no de recorrer el disco.
/// </summary>
public interface IStorageUsageProbe
{
    /// <summary>
    /// Devuelve la medición vigente, recalculándola si la cacheada expiró.
    /// </summary>
    Task<StorageUsage> MeasureAsync(CancellationToken ct = default);
}
