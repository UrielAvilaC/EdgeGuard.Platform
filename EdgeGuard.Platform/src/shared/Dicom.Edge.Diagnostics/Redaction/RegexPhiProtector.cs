using System.Text.RegularExpressions;
using Dicom.Edge.Diagnostics.Configuration;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Diagnostics.Redaction;

/// <summary>
/// Regex-based PHI redactor that sanitizes log property values according to
/// configured rules. Thread-safe for concurrent Serilog pipeline usage.
/// </summary>
public sealed class RegexPhiProtector : IPhiProtector
{
    private readonly HashSet<string> _redactedProperties;
    private readonly List<Regex> _compiledPatterns;
    private readonly string _replacementToken;
    private readonly bool _enabled;
    private readonly RedactionMode _mode;

    public RegexPhiProtector(IOptions<PhiRedactionOptions> options)
    {
        var config = options.Value;

        _enabled = config.Enabled;
        _replacementToken = config.ReplacementToken;
        _mode = config.Mode;

        _redactedProperties = new HashSet<string>(
            config.RedactedProperties,
            StringComparer.OrdinalIgnoreCase);

        _compiledPatterns = config.RedactionPatterns
            .Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
            .ToList();
    }

    /// <inheritdoc/>
    public string Redact(string? value)
    {
        if (!_enabled || string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        if (_mode == RedactionMode.Relaxed)
            return value;

        var result = value;

        foreach (var pattern in _compiledPatterns)
        {
            try
            {
                result = pattern.Replace(result, _replacementToken);
            }
            catch (RegexMatchTimeoutException)
            {
                return _replacementToken;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public bool IsPhiProperty(string propertyName)
    {
        if (!_enabled)
            return false;

        return _redactedProperties.Contains(propertyName);
    }
}
