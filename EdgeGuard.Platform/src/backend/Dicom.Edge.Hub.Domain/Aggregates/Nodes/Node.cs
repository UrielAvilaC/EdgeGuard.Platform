using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes.Events;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Aggregates.Nodes;

/// <summary>
/// Edge Node aggregate root. Represents a node registered in the Hub.
/// </summary>
public sealed class Node : AggregateRoot<string>, ISoftDeletable
{
    private readonly List<NodePacsAssignment> _pacsAssignments = [];

    public string Name { get; private set; } = default!;
    public AeTitle AeTitle { get; private set; } = default!;
    public string IpAddress { get; private set; } = default!;
    public int Port { get; private set; }
    public string? ApiEndpoint { get; private set; }
    public string? Location { get; private set; }
    public string? FacilityName { get; private set; }
    public string? TimeZone { get; private set; }
    public string? Version { get; private set; }
    public NodeStatus Status { get; private set; }
    public DateTime? LastHeartbeatAt { get; private set; }
    public bool IsEnabled { get; private set; }

    // Soft delete
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// BCrypt hash of the node's API key. Set once during registration.
    /// </summary>
    public string? ApiKeyHash { get; private set; }

    /// <summary>
    /// The same API key as <see cref="ApiKeyHash"/>, kept in reversible form and encrypted
    /// at rest (Data Protection ciphertext, <c>ENC:</c> prefix).
    ///
    /// <para>A hash only answers "is this the right key?", which is all an INBOUND node→Hub
    /// call needs. Signing an OUTBOUND Hub→Node request needs the key itself, because the node
    /// verifies an HMAC computed with it. Both fields therefore always describe the same key
    /// and must be written together — see <see cref="SetApiKey"/>.</para>
    /// </summary>
    public string? SigningSecret { get; private set; }

    /// <summary>
    /// Configurable healthcheck interval in seconds.
    /// </summary>
    public int HealthCheckIntervalSeconds { get; private set; }

    // ── Almacenamiento ────────────────────────────────────────────────────
    // Todo lo mide y reporta el nodo, salvo el límite, que el Hub administra y
    // le envía. Son nullable a propósito: null es "nunca reportado", que no es
    // lo mismo que cero. Confundirlos fue el bug original — un 0% verde se lee
    // como "hay espacio de sobra" cuando en realidad no sabemos nada.

    /// <summary>Cuota administrada desde el Hub. null = sin límite.</summary>
    public long? StorageLimitMb { get; private set; }

    /// <summary>Peso de los estudios vivos en el workspace del nodo.</summary>
    public long? StorageDicomMb { get; private set; }

    /// <summary>Peso de la base del nodo. Aparte porque purgar no la reduce.</summary>
    public long? StorageDatabaseMb { get; private set; }

    /// <summary>Libre y total del volumen: la máquina puede llenarse por causas ajenas al workspace.</summary>
    public long? StorageVolumeFreeMb { get; private set; }
    public long? StorageVolumeTotalMb { get; private set; }

    /// <summary>Cuándo midió el nodo, que no es cuándo lo recibimos.</summary>
    public DateTime? StorageMeasuredAt { get; private set; }

    /// <summary>Límite que el nodo confirmó tener aplicado, para detectar divergencia.</summary>
    public long? StorageLimitAppliedMb { get; private set; }

    // ── Configuración aplicada ────────────────────────────────────────────
    // La versión deseada se calcula al vuelo desde la configuración; aquí se
    // guarda la que el nodo confirmó, para poder mostrar "pendiente" sin
    // depender de que el push síncrono haya funcionado.
    public string? ConfigAppliedVersion { get; private set; }
    public DateTime? ConfigAppliedAt { get; private set; }

    // Metrics
    public int TotalStudiesReceived { get; private set; }
    public int TotalStudiesSent { get; private set; }
    public int ErrorsLast24Hours { get; private set; }

    public IReadOnlyList<NodePacsAssignment> PacsAssignments => _pacsAssignments.AsReadOnly();

    private Node() { }

    public static Node Create(
        string name,
        AeTitle aeTitle,
        string ipAddress,
        int port,
        string? apiEndpoint = null,
        string? location = null,
        string? facilityName = null,
        int healthCheckIntervalSeconds = 60,
        long? storageLimitMb = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Node name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address cannot be empty.", nameof(ipAddress));
        if (port < 1 || port > 65535)
            throw new ArgumentOutOfRangeException(nameof(port));

        var node = new Node
        {
            Id = IdGenerator.NewId(),
            Name = name.Trim(),
            AeTitle = aeTitle,
            IpAddress = ipAddress.Trim(),
            Port = port,
            ApiEndpoint = apiEndpoint?.Trim(),
            Location = location?.Trim(),
            FacilityName = facilityName?.Trim(),
            Status = NodeStatus.Offline,
            IsEnabled = true,
            HealthCheckIntervalSeconds = healthCheckIntervalSeconds,
            StorageLimitMb = storageLimitMb,
        };

        node.AddDomainEvent(new NodeRegisteredEvent(node.Id, name));
        return node;
    }

    public void UpdateHeartbeat(
        int? totalStudiesReceived = null,
        int? totalStudiesSent = null,
        int? errorsLast24Hours = null)
    {
        LastHeartbeatAt = DateTime.UtcNow;
        if (totalStudiesReceived.HasValue) TotalStudiesReceived = totalStudiesReceived.Value;
        if (totalStudiesSent.HasValue) TotalStudiesSent = totalStudiesSent.Value;
        if (errorsLast24Hours.HasValue) ErrorsLast24Hours = errorsLast24Hours.Value;
        UpdatedAt = DateTime.UtcNow;

        if (Status == NodeStatus.Offline)
            MarkOnline();

        AddDomainEvent(new NodeHeartbeatReceivedEvent(Id, DateTime.UtcNow));
    }

    public void MarkOnline()
    {
        var old = Status;
        Status = NodeStatus.Online;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new NodeStatusChangedEvent(Id, old, Status));
    }

    public void MarkOffline()
    {
        var old = Status;
        Status = NodeStatus.Offline;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new NodeStatusChangedEvent(Id, old, Status));
    }

    public void MarkDegraded()
    {
        var old = Status;
        Status = NodeStatus.Degraded;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new NodeStatusChangedEvent(Id, old, Status));
    }

    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignPacs(string pacsId, bool inheritedFromHub = false, int cEchoIntervalSeconds = 300)
    {
        if (_pacsAssignments.Any(a => a.PacsId == pacsId && a.IsActive))
            return;

        var assignment = NodePacsAssignment.Create(Id, pacsId, inheritedFromHub, cEchoIntervalSeconds);
        _pacsAssignments.Add(assignment);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PacsAssignedToNodeEvent(Id, pacsId, inheritedFromHub));
    }

    public void UnassignPacs(string pacsId)
    {
        var assignment = _pacsAssignments.FirstOrDefault(a => a.PacsId == pacsId && a.IsActive);
        if (assignment is null) return;

        assignment.Deactivate();
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PacsUnassignedFromNodeEvent(Id, pacsId));
    }

    public void UpdateConfiguration(
        string? location = null,
        string? facilityName = null,
        string? timeZone = null,
        string? version = null,
        int? healthCheckIntervalSeconds = null)
    {
        if (location is not null) Location = location.Trim();
        if (facilityName is not null) FacilityName = facilityName.Trim();
        if (timeZone is not null) TimeZone = timeZone.Trim();
        if (version is not null) Version = version.Trim();
        if (healthCheckIntervalSeconds.HasValue) HealthCheckIntervalSeconds = healthCheckIntervalSeconds.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Fija la cuota de almacenamiento. Se separa de UpdateConfiguration porque
    /// ahí null significa "no cambiar", y aquí tiene que poder significar "sin
    /// límite": el formulario envía siempre el estado completo, incluido el vacío.
    /// </summary>
    public void SetStorageLimit(long? limitMb)
    {
        if (limitMb is < 0)
            throw new ArgumentOutOfRangeException(nameof(limitMb), "El límite no puede ser negativo.");

        StorageLimitMb = limitMb;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Registra la medición que reportó el nodo, junto con el límite que dice
    /// tener aplicado. La divergencia entre ese eco y <see cref="StorageLimitMb"/>
    /// es lo que delata que el push de configuración aún no llegó.
    /// </summary>
    public void UpdateStorage(
        long? dicomMb,
        long? databaseMb,
        long? volumeFreeMb,
        long? volumeTotalMb,
        long? limitAppliedMb,
        DateTime? measuredAt)
    {
        StorageDicomMb = dicomMb;
        StorageDatabaseMb = databaseMb;
        StorageVolumeFreeMb = volumeFreeMb;
        StorageVolumeTotalMb = volumeTotalMb;
        StorageLimitAppliedMb = limitAppliedMb;
        StorageMeasuredAt = measuredAt ?? DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Registra la versión de configuración que el nodo confirmó aplicar.</summary>
    public void ConfirmConfigVersion(string? appliedVersion)
    {
        if (string.IsNullOrWhiteSpace(appliedVersion)) return;

        ConfigAppliedVersion = appliedVersion.Trim();
        ConfigAppliedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the API key hash. Can only be set once (immutable after first registration).
    /// </summary>
    public void SetApiKeyHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("API key hash cannot be empty.", nameof(hash));

        ApiKeyHash = hash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Stores both representations of the node's API key: the BCrypt hash used to verify
    /// inbound calls, and the encrypted key used to sign outbound pushes. Callers pass the
    /// ciphertext, never the raw key — the aggregate does not know how to encrypt.
    /// </summary>
    public void SetApiKey(string hash, string encryptedSecret)
    {
        SetApiKeyHash(hash);
        SetSigningSecret(encryptedSecret);
    }

    /// <summary>
    /// Stores the encrypted API key on its own. Used to backfill nodes that registered before
    /// <see cref="SigningSecret"/> existed: their raw key cannot be recovered from the hash, so
    /// it is captured the next time the node presents it on an authenticated call.
    /// </summary>
    public void SetSigningSecret(string encryptedSecret)
    {
        if (string.IsNullOrWhiteSpace(encryptedSecret))
            throw new ArgumentException("Signing secret cannot be empty.", nameof(encryptedSecret));

        SigningSecret = encryptedSecret;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns true if the node has an API key assigned.
    /// </summary>
    public bool HasApiKey => !string.IsNullOrEmpty(ApiKeyHash);

    /// <summary>
    /// Returns true if the Hub can sign outbound requests to this node.
    /// </summary>
    public bool HasSigningSecret => !string.IsNullOrEmpty(SigningSecret);
}
