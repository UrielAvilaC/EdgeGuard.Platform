using System.Diagnostics;

namespace Dicom.Edge.Node.Storage.Diagnostics;

/// <summary>
/// Dedicated <see cref="ActivitySource"/> for Storage-layer tracing.
/// Creates spans for file I/O, archival, integrity verification, and space calculations.
/// </summary>
internal static class StorageActivitySource
{
    public const string SourceName = "Dicom.Edge.Node.Storage";

    private static readonly ActivitySource Source = new(SourceName, "1.0.0");

    public static Activity? StartSaveInstance(string sopInstanceUid)
    {
        var activity = Source.StartActivity("Storage.SaveInstance", ActivityKind.Internal);
        activity?.SetTag("dicom.sop.uid", sopInstanceUid);
        return activity;
    }

    public static Activity? StartDeleteStudy(string studyUid)
    {
        var activity = Source.StartActivity("Storage.DeleteStudy", ActivityKind.Internal);
        activity?.SetTag("dicom.study.uid", studyUid);
        return activity;
    }

    public static Activity? StartArchiveStudy(string studyUid)
    {
        var activity = Source.StartActivity("Storage.ArchiveStudy", ActivityKind.Internal);
        activity?.SetTag("dicom.study.uid", studyUid);
        return activity;
    }

    public static Activity? StartVerifyIntegrity(string studyUid)
    {
        var activity = Source.StartActivity("Storage.VerifyIntegrity", ActivityKind.Internal);
        activity?.SetTag("dicom.study.uid", studyUid);
        return activity;
    }

    public static Activity? StartComputeChecksum(string filePath)
    {
        var activity = Source.StartActivity("Storage.ComputeChecksum", ActivityKind.Internal);
        activity?.SetTag("storage.file", filePath);
        return activity;
    }

    public static Activity? StartSpaceCheck()
        => Source.StartActivity("Storage.SpaceCheck", ActivityKind.Internal);
}
