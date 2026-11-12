using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Infrastructure;
using SeldonVigil.Workers.Workers;
using Moq;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.Workers;

/// <summary>
/// Covers the automated stage evaluators that drive certification pipelines.
/// These evaluators were previously dead code (ProgressStage existed but had no
/// caller); this worker is that caller, so its decision logic needs coverage.
/// </summary>
public class CertificationPipelineWorkerTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    public CertificationPipelineWorkerTests()
    {
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
    }

    private Agent CreateAgent()
    {
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read", "write" }
        };

        return CreateAgentWithManifest(manifest);
    }

    private Agent CreateEmptyManifestAgent() => CreateAgentWithManifest(new CapabilityManifest());

    private Agent CreateAgentWithManifest(CapabilityManifest manifest)
    {
        var result = Agent.Create(
            TenantId.New(),
            "external-123",
            "Test Agent",
            AgentFramework.LangChain,
            ModelProvider.OpenAI,
            "gpt-4",
            manifest,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        return result.Value!;
    }

    [Fact]
    public void EvaluateStaticAnalysis_NullAgent_Fails()
    {
        var evaluation = CertificationPipelineWorker.EvaluateStaticAnalysis(null);

        _ = evaluation.Success.Should().BeFalse();
        _ = evaluation.Output.Should().Contain("not found");
    }

    [Fact]
    public void EvaluateStaticAnalysis_RegisteredAgent_Passes()
    {
        // A coherent manifest (permissions + default positive rate limits) passes.
        var agent = CreateAgent();

        var evaluation = CertificationPipelineWorker.EvaluateStaticAnalysis(agent);

        _ = evaluation.Success.Should().BeTrue();
        _ = evaluation.Output.Should().Contain("coherent");
    }

    [Fact]
    public void EvaluateStaticAnalysis_EmptyManifest_Fails()
    {
        var agent = CreateEmptyManifestAgent();

        var evaluation = CertificationPipelineWorker.EvaluateStaticAnalysis(agent);

        _ = evaluation.Success.Should().BeFalse();
        _ = evaluation.Output.Should().Contain("no permissions, tools, or data-access");
    }

    [Fact]
    public void EvaluateStaticAnalysis_NonPositiveRateLimits_Fails()
    {
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" },
            RateLimits = new RateLimit { RequestsPerMinute = 0, RequestsPerHour = 1000 }
        };
        var agent = CreateAgentWithManifest(manifest);

        var evaluation = CertificationPipelineWorker.EvaluateStaticAnalysis(agent);

        _ = evaluation.Success.Should().BeFalse();
        _ = evaluation.Output.Should().Contain("rate limits");
    }

    [Fact]
    public void EvaluateStaticAnalysis_ToolWithoutName_Fails()
    {
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" },
            Tools = new List<AgentTool> { new() { Name = "" } }
        };
        var agent = CreateAgentWithManifest(manifest);

        var evaluation = CertificationPipelineWorker.EvaluateStaticAnalysis(agent);

        _ = evaluation.Success.Should().BeFalse();
        _ = evaluation.Output.Should().Contain("tool has no name");
    }

    [Fact]
    public void EvaluateStaticAnalysis_DataAccessRuleWithoutResource_Fails()
    {
        var manifest = new CapabilityManifest
        {
            Permissions = new List<string> { "read" },
            DataAccess = new List<DataAccessRule> { new() { Resource = "" } }
        };
        var agent = CreateAgentWithManifest(manifest);

        var evaluation = CertificationPipelineWorker.EvaluateStaticAnalysis(agent);

        _ = evaluation.Success.Should().BeFalse();
        _ = evaluation.Output.Should().Contain("data-access rule has no resource");
    }

    [Fact]
    public void EvaluatePolicyValidation_NullAgent_Fails()
    {
        var evaluation = CertificationPipelineWorker.EvaluatePolicyValidation(null);

        _ = evaluation.Success.Should().BeFalse();
        _ = evaluation.Output.Should().Contain("not found");
    }

    [Fact]
    public void EvaluatePolicyValidation_ActiveAgent_Passes()
    {
        var agent = CreateAgent();
        _ = agent.Activate(Guid.NewGuid(), _dateTimeProviderMock.Object);

        var evaluation = CertificationPipelineWorker.EvaluatePolicyValidation(agent);

        _ = evaluation.Success.Should().BeTrue();
    }

    [Fact]
    public void EvaluatePolicyValidation_SuspendedAgent_Fails()
    {
        var agent = CreateAgent();
        _ = agent.Activate(Guid.NewGuid(), _dateTimeProviderMock.Object);
        _ = agent.Suspend("policy breach", Guid.NewGuid(), _dateTimeProviderMock.Object);

        var evaluation = CertificationPipelineWorker.EvaluatePolicyValidation(agent);

        _ = evaluation.Success.Should().BeFalse();
        _ = evaluation.Output.Should().Contain("Suspended");
    }
}
