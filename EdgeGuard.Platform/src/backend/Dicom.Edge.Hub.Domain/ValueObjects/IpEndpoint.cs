using System.Net;

namespace Dicom.Edge.Hub.Domain.ValueObjects;

/// <summary>
/// Represents an IP address and port combination.
/// </summary>
public sealed record IpEndpoint
{
    public string Host { get; }
    public int Port { get; }

    private IpEndpoint(string host, int port)
    {
        Host = host;
        Port = port;
    }

    public static IpEndpoint Create(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host cannot be empty.", nameof(host));

        if (port < 1 || port > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");

        return new IpEndpoint(host.Trim(), port);
    }

    public override string ToString() => $"{Host}:{Port}";
}
