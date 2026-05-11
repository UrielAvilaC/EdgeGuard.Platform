using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Api.Constants;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.ValueObjects;

namespace Dicom.Edge.Hub.Api.Mapping;

/// <summary>
/// Mapping profile for Edge (node-facing) endpoints.
/// Maps <see cref="NodeRegistrationRequest"/> → <see cref="Node"/> entity
/// and provides factory methods for Edge response DTOs.
/// </summary>
public static class EdgeMappingProfile
{
    public static Node ToEntity(this NodeRegistrationRequest dto) =>
        Node.Create(
            dto.Name,
            AeTitle.Create(dto.AeTitle),
            dto.IpAddress,
            dto.Port,
            dto.ApiEndpoint,
            dto.Location,
            dto.FacilityName);

    public static HeartbeatAckDto ToHeartbeatAck() => new()
    {
        Acknowledged = true,
        ServerTimeUtc = DateTime.UtcNow
    };

    public static StudyNotifyAckDto ToStudyNotifyAck(string studyId) => new()
    {
        Acknowledged = true,
        StudyId = studyId,
        ReceivedAtUtc = DateTime.UtcNow
    };

    public static HealthReportAckDto ToHealthReportAck() => new()
    {
        Acknowledged = true
    };

    public static HubInfoDto ToHubInfo() => new()
    {
        Service = HubApiConstants.ServiceDisplayName,
        Version = HubApiConstants.ServiceVersion,
        UtcNow = DateTime.UtcNow,
        Capabilities = HubApiConstants.Capabilities
    };

    public static HubRuntimeDto ToHubRuntime() => new()
    {
        Service = HubApiConstants.ServiceName,
        UtcNow = DateTime.UtcNow,
        Environment = System.Environment.GetEnvironmentVariable(HubApiConstants.EnvironmentVariableName)
                      ?? HubApiConstants.DefaultEnvironment,
        MachineName = System.Environment.MachineName,
        Framework = System.Environment.Version.ToString()
    };
}
