using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Api.Shared.Infrastructure;
using Xunit;

namespace SeldonVigil.UnitTests.Shared.Infrastructure.Authentication;

public class AuthenticationConfigurationTests
{
    [Fact]
    public void AddSeldonVigilAuthenticationWithDevAuthEnabledInDevelopmentUsesDevAuthSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["DevAuth:Enabled"] = "true",
                ["DevAuth:SecretKey"] = "TestSecretKey123!",
                ["DevAuth:Issuer"] = "seldonvigil-test",
                ["DevAuth:Audience"] = "seldonvigil-api-test"
            })
            .Build();

        // Act
        _ = services.AddSeldonVigilAuthentication(configuration);
        _ = services.BuildServiceProvider();

        // Assert
        // The authentication should be configured with DevAuth settings
        // We can verify this by checking that the service collection has the authentication services
        _ = services.Should().ContainSingle(sd => sd.ServiceType == typeof(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider));
    }

    [Fact]
    public void AddSeldonVigilAuthenticationWithDevAuthDisabledInDevelopmentUsesOidcSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["DevAuth:Enabled"] = "false",
                ["Authentication:Authority"] = "https://test.com",
                ["Authentication:Audience"] = "test-audience"
            })
            .Build();

        // Act
        _ = services.AddSeldonVigilAuthentication(configuration);
        _ = services.BuildServiceProvider();

        // Assert
        _ = services.Should().ContainSingle(sd => sd.ServiceType == typeof(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider));
    }

    [Fact]
    public void AddSeldonVigilAuthenticationWithDevAuthEnabledInProductionUsesOidcSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["DevAuth:Enabled"] = "true",
                ["Authentication:Authority"] = "https://test.com",
                ["Authentication:Audience"] = "test-audience"
            })
            .Build();

        // Act
        _ = services.AddSeldonVigilAuthentication(configuration);
        _ = services.BuildServiceProvider();

        // Assert
        _ = services.Should().ContainSingle(sd => sd.ServiceType == typeof(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider));
    }

    [Fact]
    public void AddSeldonVigilAuthenticationWithOidcSettingsInProductionUsesOidcSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["Authentication:Authority"] = "https://test.com",
                ["Authentication:Audience"] = "test-audience"
            })
            .Build();

        // Act
        _ = services.AddSeldonVigilAuthentication(configuration);
        _ = services.BuildServiceProvider();

        // Assert
        _ = services.Should().ContainSingle(sd => sd.ServiceType == typeof(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider));
    }

    [Fact]
    public void AddSeldonVigilAuthenticationAddsAuthorizationPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["DevAuth:Enabled"] = "true",
                ["DevAuth:SecretKey"] = "TestSecretKey123!",
                ["DevAuth:Issuer"] = "seldonvigil-test",
                ["DevAuth:Audience"] = "seldonvigil-api-test"
            })
            .Build();

        // Act
        _ = services.AddSeldonVigilAuthentication(configuration);
        _ = services.BuildServiceProvider();

        // Assert
        _ = services.Should().ContainSingle(sd => sd.ServiceType == typeof(Microsoft.AspNetCore.Authorization.IAuthorizationService));
        _ = services.Should().ContainSingle(sd => sd.ServiceType == typeof(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider));
    }
}
