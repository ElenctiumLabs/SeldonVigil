using FluentAssertions;
using SeldonVigil.Api.Modules.Identity.Api.Endpoints;
using Xunit;

namespace SeldonVigil.UnitTests.Shared.Infrastructure.Authentication;

public class DevAuthOptionsTests
{
    [Fact]
    public void DevAuthOptionsWithDefaultValuesUsesDefaults()
    {
        // Arrange & Act
        var options = new DevAuthOptions();

        // Assert
        _ = options.SecretKey.Should().Be("ThisIsADevelopmentSecretKeyThatShouldBeChangedInProduction!");
        _ = options.Issuer.Should().Be("seldonvigil-dev");
        _ = options.Audience.Should().Be("seldonvigil-api");
        _ = options.Enabled.Should().BeTrue();
    }

    [Fact]
    public void DevAuthOptionsWithCustomValuesUsesCustomValues()
    {
        // Arrange & Act
        var options = new DevAuthOptions
        {
            SecretKey = "CustomSecretKey123!",
            Issuer = "custom-issuer",
            Audience = "custom-audience",
            Enabled = false
        };

        // Assert
        _ = options.SecretKey.Should().Be("CustomSecretKey123!");
        _ = options.Issuer.Should().Be("custom-issuer");
        _ = options.Audience.Should().Be("custom-audience");
        _ = options.Enabled.Should().BeFalse();
    }

    [Fact]
    public void SectionNameShouldBeDevAuth()
    {
        // Arrange & Act
        var sectionName = DevAuthOptions.SectionName;

        // Assert
        _ = sectionName.Should().Be("DevAuth");
    }
}
