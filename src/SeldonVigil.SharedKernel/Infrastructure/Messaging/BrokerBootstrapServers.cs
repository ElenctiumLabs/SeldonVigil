using System.Net;

namespace SeldonVigil.SharedKernel.Infrastructure.Messaging;

/// <summary>
/// Helpers for normalising broker bootstrap server configuration.
/// </summary>
public static class BrokerBootstrapServers
{
    /// <summary>
    /// Replaces the <c>localhost</c> host name with the IPv4 loopback
    /// (<c>127.0.0.1</c>). On Windows, <c>localhost</c> resolves to <c>::1</c>
    /// first while locally installed brokers commonly bind IPv4 only, which makes
    /// librdkafka connections intermittently fail with "all brokers down".
    /// </summary>
    public static string Normalize(string bootstrapServers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bootstrapServers);

        var entries = bootstrapServers.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < entries.Length; i++)
        {
            entries[i] = NormalizeEntry(entries[i]);
        }

        return string.Join(",", entries);
    }

    private static string NormalizeEntry(string entry)
    {
        // Handles "host:port"; bare IPv6 literals are left untouched.
        var separator = entry.LastIndexOf(':');
        if (separator <= 0)
        {
            return entry;
        }

        var host = entry[..separator].Trim();
        var port = entry[(separator + 1)..];

        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            ? $"{IPAddress.Loopback}:{port}"
            : entry;
    }
}
