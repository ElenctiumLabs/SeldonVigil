using Confluent.Kafka;

namespace SeldonVigil.SharedKernel.Infrastructure.Messaging;

/// <summary>
/// Optional TLS/SASL settings for a hardened (production) Redpanda broker.
/// Bound from the same <c>Broker</c> configuration section as
/// <see cref="BrokerConsumerOptions"/> so key names stay stable
/// (<c>Broker:SaslUsername</c>, <c>Broker:SaslPassword</c>, ...). All fields are
/// optional; a plaintext local broker works with no security configuration.
/// </summary>
public sealed class BrokerSecurityOptions
{
    public const string SectionName = BrokerConsumerOptions.SectionName;

    /// <summary><c>Plaintext</c>, <c>Ssl</c>, <c>SaslPlaintext</c> or <c>SaslSsl</c>.</summary>
    public string? SecurityProtocol { get; init; }

    /// <summary><c>SCRAM-SHA-256</c>, <c>SCRAM-SHA-512</c>, <c>Plain</c>, <c>GSSAPI</c>, ...</summary>
    public string? SaslMechanism { get; init; }

    public string? SaslUsername { get; init; }

    public string? SaslPassword { get; init; }

    /// <summary>Path to the CA certificate bundle (PEM) used to verify the broker.</summary>
    public string? SslCaLocation { get; init; }

    /// <summary>Path to a client certificate (PEM) when mutual TLS is required.</summary>
    public string? SslCertificateLocation { get; init; }

    /// <summary>Path to the client private key (PEM) when mutual TLS is required.</summary>
    public string? SslKeyLocation { get; init; }
}

public static class BrokerClientSecurity
{
    /// <summary>
    /// Applies TLS/SASL settings to any Confluent.Kafka client config
    /// (producer, consumer, and admin clients share <see cref="ClientConfig"/>).
    /// Silently no-ops for a null options instance, so plaintext development is
    /// unaffected when the security section is absent.
    /// </summary>
    public static T Apply<T>(T config, BrokerSecurityOptions? security)
        where T : ClientConfig
    {
        if (security is null)
        {
            return config;
        }

        if (!string.IsNullOrWhiteSpace(security.SecurityProtocol))
        {
            config.SecurityProtocol = ParseEnum<SecurityProtocol>(security.SecurityProtocol);
        }

        if (!string.IsNullOrWhiteSpace(security.SaslMechanism))
        {
            config.SaslMechanism = ParseEnum<SaslMechanism>(security.SaslMechanism);
        }

        if (!string.IsNullOrWhiteSpace(security.SaslUsername))
        {
            config.SaslUsername = security.SaslUsername;
        }

        if (!string.IsNullOrWhiteSpace(security.SaslPassword))
        {
            config.SaslPassword = security.SaslPassword;
        }

        if (!string.IsNullOrWhiteSpace(security.SslCaLocation))
        {
            config.SslCaLocation = security.SslCaLocation;
        }

        if (!string.IsNullOrWhiteSpace(security.SslCertificateLocation))
        {
            config.SslCertificateLocation = security.SslCertificateLocation;
        }

        if (!string.IsNullOrWhiteSpace(security.SslKeyLocation))
        {
            config.SslKeyLocation = security.SslKeyLocation;
        }

        return config;
    }

    // Confluent.Kafka enum members are PascalCase with no separators
    // ("SaslSsl", "ScramSha256") while operators write them as
    // "SASL_SSL", "SCRAM-SHA-256". Normalize both sides and compare
    // case-insensitively so common spellings from config/env work.
    private static TE ParseEnum<TE>(string value)
        where TE : struct, Enum
    {
        var normalized = value
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        var matchingName = Enum.GetNames<TE>()
            .FirstOrDefault(name => string.Equals(
                name.Replace("_", string.Empty, StringComparison.Ordinal),
                normalized,
                StringComparison.OrdinalIgnoreCase));

        return matchingName is not null
            ? Enum.Parse<TE>(matchingName, ignoreCase: false)
            : throw new ArgumentException($"'{value}' is not a valid {typeof(TE).Name}.", nameof(value));
    }
}
