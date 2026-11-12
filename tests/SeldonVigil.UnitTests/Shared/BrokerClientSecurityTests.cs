using Confluent.Kafka;
using FluentAssertions;
using SeldonVigil.SharedKernel.Infrastructure.Messaging;
using Xunit;

namespace SeldonVigil.UnitTests.Shared.Infrastructure.Messaging;

public class BrokerClientSecurityTests
{
    [Fact]
    public void ApplyWithNullSecurityLeavesConfigUntouched()
    {
        var config = new ProducerConfig { BootstrapServers = "localhost:9092" };
        BrokerClientSecurity.Apply(config, null).Should().BeSameAs(config);
        config.SaslMechanism.Should().BeNull();
        config.SecurityProtocol.Should().BeNull();
    }

    [Fact]
    public void ApplyMapsSaslScramKeysToConfluentEnums()
    {
        var security = new BrokerSecurityOptions
        {
            SecurityProtocol = "SASL_SSL",
            SaslMechanism = "SCRAM-SHA-256",
            SaslUsername = "seldonvigil-api",
            SaslPassword = "s3cret",
            SslCaLocation = "/etc/ssl/redpanda/ca.crt"
        };

        var config = BrokerClientSecurity.Apply(new ConsumerConfig { BootstrapServers = "redpanda:9093" }, security);

        config.SecurityProtocol.Should().Be(SecurityProtocol.SaslSsl);
        config.SaslMechanism.Should().Be(SaslMechanism.ScramSha256);
        config.SaslUsername.Should().Be("seldonvigil-api");
        config.SaslPassword.Should().Be("s3cret");
        config.SslCaLocation.Should().Be("/etc/ssl/redpanda/ca.crt");
    }

    [Fact]
    public void ApplyIgnoresBlankSecurityFields()
    {
        var security = new BrokerSecurityOptions { SaslUsername = "seldonvigil-workers" };

        var config = BrokerClientSecurity.Apply(new AdminClientConfig(), security);

        config.SecurityProtocol.Should().BeNull();
        config.SaslMechanism.Should().BeNull();
        config.SaslUsername.Should().Be("seldonvigil-workers");
        config.SaslPassword.Should().BeNull();
    }

    [Fact]
    public void ApplyWorksForProducerConsumerAndAdminClients()
    {
        var security = new BrokerSecurityOptions
        {
            SecurityProtocol = "SASL_SSL",
            SaslMechanism = "PLAIN",
            SaslUsername = "u",
            SaslPassword = "p"
        };

        BrokerClientSecurity.Apply(new ProducerConfig(), security).SecurityProtocol.Should().Be(SecurityProtocol.SaslSsl);
        BrokerClientSecurity.Apply(new ConsumerConfig(), security).SecurityProtocol.Should().Be(SecurityProtocol.SaslSsl);
        BrokerClientSecurity.Apply(new AdminClientConfig(), security).SecurityProtocol.Should().Be(SecurityProtocol.SaslSsl);
    }
}
