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
using SeldonVigil.Shared.Domain.Saga;

namespace SeldonVigil.UnitTests.Identity.Application.Commands;

public class ActivateAgentCommandHandlerTests
{
    private readonly Mock<IAgentRepository> _agentRepositoryMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<IRequestContext> _requestContextMock;
    private readonly Mock<ISagaOrchestrator> _sagaOrchestratorMock;
    private readonly ActivateAgentCommandHandler _handler;

    public ActivateAgentCommandHandlerTests()
    {
        _agentRepositoryMock = new Mock<IAgentRepository>();
        _eventBusMock = new Mock<IEventBus>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _requestContextMock = new Mock<IRequestContext>();
        _sagaOrchestratorMock = new Mock<ISagaOrchestrator>();
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _ = _requestContextMock.Setup(x => x.TenantId).Returns(Guid.NewGuid().ToString());
        _ = _requestContextMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var logger = new Mock<ILogger<ActivateAgentCommandHandler>>();
        var sagaSteps = new List<ISagaStep>
        {
            new Mock<ISagaStep>().Object,
        };
        _handler = new ActivateAgentCommandHandler(
            _agentRepositoryMock.Object,
            _eventBusMock.Object,
            _dateTimeProviderMock.Object,
            _requestContextMock.Object,
            _sagaOrchestratorMock.Object,
            sagaSteps,
            logger.Object);
    }

    [Fact]
    public async Task HandleValidActivationReturnsSuccess()
    {
        // Arrange
        var agentId = AgentId.New();
        var command = new ActivateAgentCommand(agentId);
        var tenantId = TenantId.New();

        var agent = CreateTestAgent(tenantId, agentId, AgentStatus.Draft);

        _ = _agentRepositoryMock.Setup(x => x.GetByIdAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _agentRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Agent>(), It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleNonExistentAgentReturnsNotFound()
    {
        // Arrange
        var agentId = AgentId.New();
        var command = new ActivateAgentCommand(agentId);

        _ = _agentRepositoryMock.Setup(x => x.GetByIdAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Agent?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        result.Match(
            _ => throw new Exception("Expected failure"),
            error => error.Type.Should().Be(ErrorType.NotFound));
        _agentRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Agent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAlreadyActiveAgentReturnsFailure()
    {
        // Arrange
        var agentId = AgentId.New();
        var command = new ActivateAgentCommand(agentId);
        var tenantId = TenantId.New();

        var agent = CreateTestAgent(tenantId, agentId, AgentStatus.Active);

        _ = _agentRepositoryMock.Setup(x => x.GetByIdAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
    }

    private static Agent CreateTestAgent(TenantId tenantId, AgentId agentId, AgentStatus status)
    {
        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var createResult = Agent.Create(
            tenantId,
            "ext-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            new CapabilityManifest { Permissions = ["read"] },
            Guid.NewGuid(),
            dateTimeProviderMock.Object);

        var agent = createResult.Value!;
        if (status == AgentStatus.Active)
        {
            _ = agent.Activate(Guid.NewGuid(), dateTimeProviderMock.Object);
        }

        return agent;
    }
}
