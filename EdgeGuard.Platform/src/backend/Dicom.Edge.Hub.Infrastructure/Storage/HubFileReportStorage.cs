using Dicom.Edge.Hub.Application.Reports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Infrastructure.Storage;

/// <summary>
/// File-system implementation of <see cref="IReportStorage"/> that writes report PDFs
/// under the configured Hub workspace. Filenames are sanitized and every resolved path
/// is validated to stay within the workspace root (anti path-traversal).
/// </summary>
public sealed class HubFileReportStorage : IReportStorage
{
    private readonly HubWorkspaceOptions _options;
    private readonly ILogger<HubFileReportStorage> _logger;

    public HubFileReportStorage(IOptions<HubWorkspaceOptions> options, ILogger<HubFileReportStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    private string Root => Path.GetFullPath(_options.ReportsPath);

    public async Task<string> SavePdfAsync(string studyId, byte[] pdf, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Root);

        var fileName = $"{Sanitize(studyId)}.pdf";
        var fullPath = ResolveWithinRoot(fileName);

        await File.WriteAllBytesAsync(fullPath, pdf, ct);
        _logger.LogInformation(
            "Stored report PDF for study {StudyId} ({Bytes} bytes) at {Path}",
            studyId, pdf.Length, fileName);

        return fileName; // relative path persisted on the study
    }

    public Task<Stream?> OpenPdfAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = ResolveWithinRoot(Sanitize(relativePath));
        Stream? stream = File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        return Task.FromResult(stream);
    }

    private string ResolveWithinRoot(string fileName)
    {
        var fullPath = Path.GetFullPath(Path.Combine(Root, fileName));
        if (!fullPath.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullPath, Root, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Resolved path escapes the report workspace root.");
        return fullPath;
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Replace("..", "_");
    }
}
