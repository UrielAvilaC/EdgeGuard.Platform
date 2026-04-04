using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Node.Sender;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Router;

/// <summary>
/// Rule-based study router. Evaluates configured routing rules against study metadata
/// and returns matching PACS destinations ordered by rule priority.
/// Falls back to a default destination when no rules match.
/// </summary>
public sealed class RuleBasedStudyRouter(
    INodeSettingsService settingsService,
    ILogger<RuleBasedStudyRouter> logger) : IStudyRouter
{
    // In-memory rules — loaded from node settings or configuration sync
    private readonly List<RoutingRule> _rules = [];
    private PacsDestination? _defaultDestination;

    public void LoadRules(IEnumerable<RoutingRule> rules, PacsDestination? defaultDestination = null)
    {
        _rules.Clear();
        _rules.AddRange(rules.OrderBy(r => r.Priority));
        _defaultDestination = defaultDestination;
        logger.LogInformation("Loaded {Count} routing rules", _rules.Count);
    }

    public Task<IReadOnlyList<PacsDestination>> ResolveDestinationsAsync(
        StudyRoutingContext context,
        CancellationToken ct = default)
    {
        var matched = _rules
            .Where(r => r.Matches(context))
            .Select(r => r.Destination)
            .Distinct()
            .ToList();

        if (matched.Count == 0 && _defaultDestination is not null)
        {
            logger.LogDebug(
                "No routing rule matched for study {StudyUid}, using default destination {AeTitle}",
                context.StudyInstanceUid, _defaultDestination.AeTitle);
            matched.Add(_defaultDestination);
        }

        if (matched.Count == 0)
        {
            logger.LogWarning("No destination found for study {StudyUid}", context.StudyInstanceUid);
        }
        else
        {
            logger.LogDebug(
                "Resolved {Count} destination(s) for study {StudyUid}",
                matched.Count, context.StudyInstanceUid);
        }

        return Task.FromResult<IReadOnlyList<PacsDestination>>(matched);
    }
}
