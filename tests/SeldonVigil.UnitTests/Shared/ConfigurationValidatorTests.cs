using Microsoft.Extensions.Configuration;
using SeldonVigil.Api.Shared.Infrastructure.Authentication;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.Shared.Infrastructure.Authentication;

public class ConfigurationValidatorTests
{
    private readonly ConfigurationValidator _validator = new();

    [Fact]
    public void ValidateWithMissingAuthorityReturnsFailure()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["Authentication:Audience"] = "test-audience"
            })
            .Build();

        // Act
        var result = _validator.Validate(configuration);

        // Assert
        _ = result.IsValid.Should().BeFalse();
        _ = result.ErrorMessage.Should().Contain("Authentication:Authority");
    }

    [Fact]
    public void ValidateWithMissingAudienceReturnsFailure()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["Authentication:Authority"] = "https://test.com"
            })
            .Build();

        // Act
        var result = _validator.Validate(configuration);

        // Assert
        _ = result.IsValid.Should().BeFalse();
        _ = result.ErrorMessage.Should().Contain("Authentication:Audience");
    }

    [Fact]
    public void ValidateWithMissingPostgresConnectionStringReturnsFailure()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["Authentication:Authority"] = "https://test.com",
                ["Authentication:Audience"] = "test-audience"
            })
            .Build();

        // Act
        var result = _validator.Validate(configuration);

        // Assert
        _ = result.IsValid.Should().BeFalse();
        _ = result.ErrorMessage.Should().Contain("ConnectionStrings:PostgreSQL");
    }

    [Fact]
    public void ValidateWithMissingRedisConnectionStringReturnsFailure()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["Authentication:Authority"] = "https://test.com",
                ["Authentication:Audience"] = "test-audience",
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test"
            })
            .Build();

        // Act
        var result = _validator.Validate(configuration);

        // Assert
        _ = result.IsValid.Should().BeFalse();
        _ = result.ErrorMessage.Should().Contain("ConnectionStrings:Redis");
    }

    [Fact]
    public void ValidateWithMissingBrokerConnectionStringReturnsFailure()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["Authentication:Authority"] = "https://test.com",
                ["Authentication:Audience"] = "test-audience",
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["ConnectionStrings:Redis"] = "localhost:6379"
            })
            .Build();

        // Act
        var result = _validator.Validate(configuration);

        // Assert
        _ = result.IsValid.Should().BeFalse();
        _ = result.ErrorMessage.Should().Contain("ConnectionStrings:Broker");
    }

    [Fact]
    public void ValidateWithMissingKeyVaultInProductionReturnsSuccess()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["Authentication:Authority"] = "https://idp.test.com",
                ["Authentication:Audience"] = "seldonvigil-api",
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["ConnectionStrings:Broker"] = "redpanda.prod:9093",
                ["Broker:SecurityProtocol"] = "SASL_SSL",
                ["Broker:SaslMechanism"] = "SCRAM-SHA-256",
                ["Broker:SaslUsername"] = "seldonvigil-api",
                ["Broker:SaslPassword"] = "s3cret",
                ["Broker:SslCaLocation"] = "/etc/ssl/redpanda/ca.crt"
            })
            .Build();

        // Act
        var result = _validator.Validate(configuration);

        // Assert
        _ = result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateWithMissingKeyVaultInDevelopmentReturnsSuccess()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["Authentication:Authority"] = "https://test.com",
                ["Authentication:Audience"] = "test-audience",
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["ConnectionStrings:Broker"] = "localhost:9092"
            })
            .Build();

        // Act
        var result = _validator.Validate(configuration);

        // Assert
        _ = result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateWithDevAuthEnabledInDevelopmentRequiresDevAuthSettings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["DevAuth:Enabled"] = "true",
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["ConnectionStrings:Broker"] = "localhost:9092"
            })
            .Build();

        // Act
        var result = _validator.Validate(configuration);

        // Assert
        _ = result.IsValid.Should().BeFalse();
        _ = result.ErrorMessage.Should().Contain("DevAuth:SecretKey");
    }

    [Fact]
    public void ValidateWithDevAuthEnabledAndValidSettingsReturnsSuccess()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["DevAuth:Enabled"] = "true",
                ["DevAuth:SecretKey"] = "test-secret-key",
                ["DevAuth:Issuer"] = "seldonvigil-dev",
                ["DevAuth:Audience"] = "seldonvigil-api",
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["ConnectionStrings:Broker"] = "localhost:9092"
            })
            .Build();

        // Act
        var result = _validator.Validate(configuration);

        // Assert
        _ = result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateWithAllRequiredConfigurationReturnsSuccess()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["Authentication:Authority"] = "https://test.com",
                ["Authentication:Audience"] = "test-audience",
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["ConnectionStrings:Broker"] = "localhost:9092",
                ["Azure:KeyVaultName"] = "test-vault"
            })
            .Build();

        // Act
        var result = _validator.Validate(configuration);

        // Assert
        _ = result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateProductionMissingSecurityProtocolReturnsFailure()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["Authentication:Authority"] = "https://idp.test.com",
                ["Authentication:Audience"] = "seldonvigil-api",
                ["Azure:KeyVaultName"] = "seldonvigil-vault",
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["ConnectionStrings:Broker"] = "redpanda.prod:9093"
            })
            .Build();

        // Act
        var result = _validator.Validate(configuration);

        // Assert
        _ = result.IsValid.Should().BeFalse();
        _ = result.ErrorMessage.Should().Contain("Broker:SecurityProtocol");
    }

    [Fact]
    public void ValidateProductionWithAllSaslTlsKeysReturnsSuccess()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["Authentication:Authority"] = "https://idp.test.com",
                ["Authentication:Audience"] = "seldonvigil-api",
                ["Azure:KeyVaultName"] = "seldonvigil-vault",
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["ConnectionStrings:Broker"] = "redpanda.prod:9093",
                ["Broker:SecurityProtocol"] = "SASL_SSL",
                ["Broker:SaslMechanism"] = "SCRAM-SHA-256",
                ["Broker:SaslUsername"] = "seldonvigil-api",
                ["Broker:SaslPassword"] = "s3cret",
                ["Broker:SslCaLocation"] = "/etc/ssl/redpanda/ca.crt"
            })
            .Build();

        // Act
        var result = _validator.Validate(configuration);

        // Assert
        _ = result.IsValid.Should().BeTrue();
    }
}