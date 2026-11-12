using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Security.Application.Commands;

internal sealed record EvaluateInputCommand(
    Guid TenantId,
    string AgentId,
    string OperationType,
    Dictionary<string, string> Input,
    Dictionary<string, string> Context) : ICommand<EvaluateInputResult>;

internal sealed record InputPolicyViolation(
    string PolicyId,
    string PolicyName,
    string Severity,
    string Message);

internal sealed record EvaluateInputResult(
    bool IsAllowed,
    IReadOnlyList<InputPolicyViolation> Violations);

internal sealed partial class EvaluateInputCommandHandler(
    ISecurityPolicyRepository policyRepository,
    IDateTimeProvider dateTimeProvider,
    ILogger<EvaluateInputCommandHandler> logger)
    : ICommandHandler<EvaluateInputCommand, EvaluateInputResult>
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Input evaluation for agent {AgentId}: {ViolationCount} violation(s)")]
    private static partial void LogInputEvaluated(ILogger logger, string agentId, int violationCount);

    public async Task<Result<EvaluateInputResult>> Handle(EvaluateInputCommand request, CancellationToken ct)
    {
        var tenantId = new TenantId(request.TenantId);
        var policies = await policyRepository
            .GetActiveByTenantAsync(tenantId, ct)
            .ConfigureAwait(false);

        var now = dateTimeProvider.UtcNow;
        var violations = new List<InputPolicyViolation>();

        foreach (var policy in policies)
        {
            // In-memory synthetic trace so RegexMatch/threshold rules can inspect the
            // submitted content without persisting anything.
            var probe = Trace.Create(
                tenantId,
                new AgentId(Guid.NewGuid()),
                parentTraceId: null,
                operationName: "security.evaluate-input",
                operationType: ParseOperationType(request.OperationType),
                startTime: now.AddSeconds(-1),
                endTime: now,
                status: TraceStatus.Ok,
                attributes: MergeAttributes(request),
                events: [],
                links: [],
                tokenUsage: new TokenUsage(0, 0),
                cost: new Cost(0m));

            if (probe.IsFailure || !policy.RuleDefinition.Evaluate(probe.Value!))
            {
                continue;
            }

            violations.Add(new InputPolicyViolation(
                probe.Value!.Id.ToString(),
                policy.Name,
                policy.Severity.ToString(),
                $"Input violated security policy '{policy.Name}'."));
        }

        LogInputEvaluated(logger, request.AgentId, violations.Count);
        return Result<EvaluateInputResult>.Success(new EvaluateInputResult(violations.Count == 0, violations));
    }

    private static Dictionary<string, string> MergeAttributes(EvaluateInputCommand request)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in request.Input)
        {
            attributes[key] = value;
        }

        foreach (var (key, value) in request.Context)
        {
            attributes[$"ctx_{key}"] = value;
        }

        attributes["agent_id"] = request.AgentId;
        attributes["operation_type"] = request.OperationType;
        return attributes;
    }

    private static OperationType ParseOperationType(string operationType) =>
        Enum.TryParse(operationType, ignoreCase: true, out OperationType parsed) ? parsed : OperationType.Custom;
}
