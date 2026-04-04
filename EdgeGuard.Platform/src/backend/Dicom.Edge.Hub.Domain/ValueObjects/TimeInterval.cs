namespace Dicom.Edge.Hub.Domain.ValueObjects;

/// <summary>
/// Configurable time interval value object (e.g., healthcheck every N seconds).
/// </summary>
public sealed record TimeInterval
{
    public int Seconds { get; }

    private TimeInterval(int seconds) => Seconds = seconds;

    public static TimeInterval FromSeconds(int seconds)
    {
        if (seconds < 1)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Interval must be at least 1 second.");

        return new TimeInterval(seconds);
    }

    public static TimeInterval FromMinutes(int minutes) =>
        FromSeconds(minutes * 60);

    public TimeSpan ToTimeSpan() => TimeSpan.FromSeconds(Seconds);

    public override string ToString() => $"{Seconds}s";
}
