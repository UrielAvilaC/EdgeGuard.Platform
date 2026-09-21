using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Nodes;

/// <summary>
/// Represents the assignment of a PACS server to a node, with C-ECHO tracking.
/// </summary>
public sealed class NodePacsAssignment : Entity<string>
{
    public string NodeId { get; private set; } = default!;
    public string PacsId { get; private set; } = default!;
    public DateTime AssignedAt { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// Whether this assignment was inherited from Hub-level global PACS config.
    /// </summary>
    public bool InheritedFromHub { get; private set; }

    // ── Seguimiento C-ECHO ───────────────────────────────────────────────────
    //
    // Aquí es donde pertenece la conectividad, y no en PacsServer: la sondea el nodo
    // con C-ECHO contra los destinos que tiene asignados, así que es una propiedad del
    // par (nodo, PACS). El mismo PACS puede estar vivo para un nodo y muerto para otro
    // —distinta red, distinto firewall, distinto AE llamado— y uno sin nodos asignados
    // no tiene alcanzabilidad definida, porque nadie lo sondea.

    public DateTime? LastCEchoAt { get; private set; }
    public bool? LastCEchoSuccess { get; private set; }

    /// <summary>Latencia del último C-ECHO exitoso, en milisegundos.</summary>
    public double? LastCEchoLatencyMs { get; private set; }

    /// <summary>Error legible del último C-ECHO fallido (red, timeout, etc.).</summary>
    public string? LastCEchoError { get; private set; }

    /// <summary>Rechazo DICOM estructurado, p. ej. "CalledAENotRecognized".</summary>
    public string? LastCEchoErrorReason { get; private set; }

    public int CEchoIntervalSeconds { get; private set; }

    private NodePacsAssignment() { }

    public static NodePacsAssignment Create(
        string nodeId,
        string pacsId,
        bool inheritedFromHub = false,
        int cEchoIntervalSeconds = 300)
    {
        return new NodePacsAssignment
        {
            Id = IdGenerator.NewId(),
            NodeId = nodeId,
            PacsId = pacsId,
            AssignedAt = DateTime.UtcNow,
            IsActive = true,
            InheritedFromHub = inheritedFromHub,
            CEchoIntervalSeconds = cEchoIntervalSeconds
        };
    }

    /// <summary>
    /// Registra el resultado del último sondeo C-ECHO que el nodo reportó para este
    /// destino.
    /// </summary>
    /// <param name="checkedAtUtc">
    /// Instante en que el <b>nodo</b> hizo el sondeo, no en que el Hub lo recibió. La
    /// diferencia importa: el nodo reporta por lotes cada cierto intervalo, así que usar
    /// la hora de recepción envejecería mal la medición en la pantalla.
    /// </param>
    public void UpdateCEchoResult(
        bool success,
        DateTime checkedAtUtc,
        double? latencyMs = null,
        string? error = null,
        string? errorReason = null)
    {
        LastCEchoAt = checkedAtUtc;
        LastCEchoSuccess = success;

        // En el caso exitoso no hay error que conservar, y al revés: mantener el error
        // anterior junto a un éxito nuevo haría que la pantalla mostrara un motivo de
        // fallo que ya no corresponde.
        LastCEchoLatencyMs   = success ? latencyMs : null;
        LastCEchoError       = success ? null : error;
        LastCEchoErrorReason = success ? null : errorReason;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
