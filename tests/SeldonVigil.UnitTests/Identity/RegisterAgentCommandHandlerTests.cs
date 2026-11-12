using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SeldonVigil.Api.Modules.Identity.Application.Commands;
using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.UnitTests.Identity.Application.Commands;

public class RegisterAgentCommandHandlerTests
{
    private readonly Mock<IAgentRepository> _agentRepositoryMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<IRequestContext> _requestContextMock;
    private readonly RegisterAgentCommandHandler _handler;

    public RegisterAgentCommandHandlerTests()
    {
        _agentRepositoryMock = new Mock<IAgentRepository>();
        _eventBusMock = new Mock<IEventBus>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _requestContextMock = new Mock<IRequestContext>();
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _ = _requestContextMock.Setup(x => x.TenantId).Returns(Guid.NewGuid().ToString());
        _ = _requestContextMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var logger = new Mock<ILogger<RegisterAgentCommandHandler>>();
        _handler = new RegisterAgentCommandHandler(
            _agentRepositoryMock.Object,
            _eventBusMock.Object,
            _dateTimeProviderMock.Object,
            _requestContextMock.Object,
            logger.Object);
    }

    [Fact]
    public async Task HandleValidRequestReturnsSuccess()
    {
        // Arrange
        var command = new RegisterAgentCommand(
            "ext-123",
            "Test Agent",
            "A test agent",
            AgentFramework.LangChain,
            null,
            ModelProvider.OpenAI,
            "gpt-4",
            null,
            new CapabilityManifest { Permissions = ["read"] });

        _ = _agentRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<TenantId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        result.Match(
            agentId => agentId.Should().NotBe(Guid.Empty),
            _ => throw new Exception("Expected success"));
        _agentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Agent>(), It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleDuplicateExternalIdReturnsConflict()
    {
        // Arrange
        var command = new RegisterAgentCommand(
            "ext-123",
            "Test Agent",
            null,
            AgentFramework.LangChain,
            null,
            ModelProvider.OpenAI,
            "gpt-4",
            null,
            new CapabilityManifest());

        _ = _agentRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<TenantId>(), "ext-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        result.Match(
            _ => throw new Exception("Expected failure"),
            error => error.Type.Should().Be(ErrorType.Conflict));
        _agentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Agent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWithoutTenantIdReturnsUnauthorized()
    {
        // Arrange
        _ = _requestContextMock.Setup(x => x.TenantId).Returns((string?)null);
        var command = new RegisterAgentCommand(
            "ext-123",
            "Test Agent",
            null,
            AgentFramework.LangChain,
            null,
            ModelProvider.OpenAI,
            "gpt-4",
            null,
            new CapabilityManifest());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        result.Match(
            _ => throw new Exception("Expected failure"),
            error => error.Type.Should().Be(ErrorType.Unauthorized));
    }
}
