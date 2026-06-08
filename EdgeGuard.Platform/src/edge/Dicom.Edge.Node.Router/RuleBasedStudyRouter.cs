using Dicom.Edge.Node.Sender;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Router;

/// <summary>
/// Rule-based study router. Evaluates configured routing rules against study metadata
/// and returns matching PACS destinations ordered by rule priority.
/// Falls back to a default destination when no rules match.
/// </summary>
public sealed class RuleBasedStudyRouter(
    ILogger<RuleBasedStudyRouter> logger) : IStudyRouter
{
    // In-memory rules — loaded from node settings or configuration sync
    private readonly List<RoutingRule> _rules = [];
    private readonly List<PacsDestination> _defaultDestinations = [];

    public void LoadRules(IEnumerable<RoutingRule> rules, IEnumerable<PacsDestination>? defaultDestinations = null)
    {
        _rules.Clear();
        _rules.AddRange(rules.OrderBy(r => r.Priority));

        _defaultDestinations.Clear();
        if (defaultDestinations is not null)
            _defaultDestinations.AddRange(defaultDestinations);

        logger.LogInformation(
            "Loaded {Count} routing rules, {DefaultCount} default destination(s): [{Aes}]",
            _rules.Count,
            _defaultDestinations.Count,
            string.Join(", ", _defaultDestinations.Select(d => d.AeTitle)));
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

        if (matched.Count == 0 && _defaultDestinations.Count > 0)
        {
            logger.LogDebug(
                "No routing rule matched for study {StudyUid}, using {Count} default destination(s): [{Aes}]",
                context.StudyInstanceUid,
                _defaultDestinations.Count,
                string.Join(", ", _defaultDestinations.Select(d => d.AeTitle)));
            matched.AddRange(_defaultDestinations);
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
