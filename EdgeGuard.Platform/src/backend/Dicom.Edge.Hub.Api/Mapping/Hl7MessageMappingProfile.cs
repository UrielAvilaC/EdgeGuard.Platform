using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Entities;

namespace Dicom.Edge.Hub.Api.Mapping;

/// <summary>
/// Mapping profile for <see cref="Hl7Message"/> → <see cref="Hl7MessageDto"/>
/// and <see cref="Hl7Message"/> → <see cref="Hl7MessageQueuedDto"/>.
/// Multiple DTOs per entity: full detail vs. queued summary.
/// </summary>
public static class Hl7MessageMappingProfile
{
    public static Hl7MessageDto ToDto(this Hl7Message entity) => new()
    {
        Id = entity.Id,
        MessageType = entity.MessageType,
        TriggerEvent = entity.TriggerEvent,
        PatientId = entity.PatientId,
        PatientName = entity.PatientName,
        SendingFacility = entity.SendingFacility,
        Status = entity.Status.ToString(),
        DispatchStatus = entity.DispatchStatus.ToString(),
        TargetNodeId = entity.TargetNodeId,
        TargetNodeName = entity.TargetNodeName,
        Priority = entity.Priority,
        DispatchAttempts = entity.DispatchAttempts,
        DispatchError = entity.DispatchError,
        ReceivedAt = entity.ReceivedAt,
        ValidatedAt = entity.ValidatedAt,
        RoutedAt = entity.RoutedAt,
        QueuedAt = entity.QueuedAt,
        DispatchedAt = entity.DispatchedAt,
        DeliveredAt = entity.DeliveredAt
    };

    public static Hl7MessageQueuedDto ToQueuedDto(this Hl7Message entity) => new()
    {
        Id = entity.Id,
        MessageType = entity.MessageType,
        TriggerEvent = entity.TriggerEvent,
        PatientId = entity.PatientId,
        TargetNodeId = entity.TargetNodeId,
        TargetNodeName = entity.TargetNodeName,
        Priority = entity.Priority,
        ReceivedAt = entity.ReceivedAt,
        QueuedAt = entity.QueuedAt
    };
}
