namespace Dicom.Edge.Hub.Application.Studies;

public interface IStudyRequeuePushService
{
    /// <summary>
    /// Pushes a manual resend request for <paramref name="studyInstanceUid"/> to the node
    /// identified by <paramref name="nodeId"/>, targeting only <paramref name="pacsIds"/>.
    /// </summary>
    Task<bool> PushAsync(
        string nodeId, string studyInstanceUid, IReadOnlyList<string> pacsIds, CancellationToken ct = default);
}
