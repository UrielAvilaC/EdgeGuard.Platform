using Dicom.Edge.Diagnostics.Configuration;
using Dicom.Edge.Diagnostics.Redaction;
using Microsoft.Extensions.Options;
using Serilog.Core;
using Serilog.Events;

namespace Dicom.Edge.Diagnostics.Enrichers;

/// <summary>
/// Enricher that inspects log event properties and redacts values
/// classified as PHI based on configured rules and patterns.
/// Thread-safe for concurrent Serilog pipeline usage.
/// </summary>
public sealed class PhiRedactionEnricher : ILogEventEnricher
{
    private readonly IPhiProtector _protector;
    private readonly string _replacementToken;
    private readonly bool _enabled;

    public PhiRedactionEnricher(IPhiProtector protector, IOptions<PhiRedactionOptions> options)
    {
        _protector = protector;
        _replacementToken = options.Value.ReplacementToken;
        _enabled = options.Value.Enabled;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!_enabled)
            return;

        List<LogEventProperty>? propertiesToRedact = null;

        foreach (var property in logEvent.Properties)
        {
            if (_protector.IsPhiProperty(property.Key))
            {
                propertiesToRedact ??= [];
                propertiesToRedact.Add(
                    new LogEventProperty(property.Key, new ScalarValue(_replacementToken)));
                continue;
            }

            if (property.Value is ScalarValue { Value: string stringValue })
            {
                var redacted = _protector.Redact(stringValue);
                if (!ReferenceEquals(redacted, stringValue) && redacted != stringValue)
                {
                    propertiesToRedact ??= [];
                    propertiesToRedact.Add(
                        new LogEventProperty(property.Key, new ScalarValue(redacted)));
                }
            }
        }

        if (propertiesToRedact is not null)
        {
            foreach (var property in propertiesToRedact)
            {
                logEvent.AddOrUpdateProperty(property);
            }
        }
    }
}
