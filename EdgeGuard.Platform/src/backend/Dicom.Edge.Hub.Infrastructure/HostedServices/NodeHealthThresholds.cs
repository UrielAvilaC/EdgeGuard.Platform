namespace Dicom.Edge.Hub.Infrastructure.HostedServices;

/// <summary>
/// Umbrales a partir de los cuales un nodo se considera degradado.
///
/// <para>Están fuera del código porque lo razonable depende del sitio: un nodo
/// con cuota chica vive normalmente al 85%, y otro con disco de sobra debería
/// alertar mucho antes.</para>
/// </summary>
public sealed class NodeHealthThresholds
{
    public const string SectionName = "NodeHealthThresholds";

    /// <summary>Uso de CPU sostenido, en porcentaje.</summary>
    public double CpuPercent { get; set; } = 90;

    /// <summary>Estudios en cola esperando despacho.</summary>
    public int QueueDepth { get; set; } = 100;

    /// <summary>
    /// Porcentaje del límite de almacenamiento consumido. Relativo, no absoluto:
    /// un umbral fijo en MB no significa nada sin saber la cuota — 1 GB usado es
    /// todo para un nodo de 2 GB y nada para uno de 500 GB.
    /// </summary>
    public double StoragePercent { get; set; } = 90;
}
