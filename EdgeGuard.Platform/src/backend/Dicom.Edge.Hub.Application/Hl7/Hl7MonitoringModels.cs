using Dicom.Edge.Hub.Domain.Entities;

namespace Dicom.Edge.Hub.Application.Hl7;

public sealed record Hl7ListenerStatusDto(bool IsRunning, int Port, int ActiveConnections);

public sealed record Hl7MessageSummaryDto(
    Guid Id,
    string MessageType,
    string? SendingApplication,
    string? SendingFacility,
    DateTime ReceivedAt,
    string? ClientEndpoint,
    Hl7MessageStatus Status,
    DateTime? ProcessedAt,
    string? ErrorMessage);

public sealed record Hl7MessageDetailDto(
    Guid Id,
    string Content,
    string MessageType,
    string? SendingApplication,
    string? SendingFacility,
    DateTime ReceivedAt,
    string? ClientEndpoint,
    Hl7MessageStatus Status,
    DateTime? ProcessedAt,
    string? ErrorMessage);
