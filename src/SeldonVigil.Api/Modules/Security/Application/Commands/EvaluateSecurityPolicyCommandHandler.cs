using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.Events;
using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Application;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Security.Application.Commands;

internal sealed partial class EvaluateSecurityPolicyCommandHandler(
    ISecurityPolicyRepository policyRepository,
    IEventBus eventBus,
    ICacheService cacheService,
    IDateTimeProvider dateTimeProvider,
    ILogger<EvaluateSecurityPolicyCommandHandler> logger)
    : Shared.Application.ICommandHandler<EvaluateSecurityPolicyCommand>
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Security evaluation completed for trace {TraceId}: {ViolationCount} violations detected")]
    private static partial void LogViolationsDetected(ILogger logger, string traceId, int violationCount);

    public async Task<Result<Unit>> Handle(EvaluateSecurityPolicyCommand request, CancellationToken ct)
    {
        // Get active policies for tenant (cached)
        var cacheKey = $"seldonvigil:policies:{request.Attributes.GetValueOrDefault("tenant_id", "unknown")}";
        var policies = await cacheService.GetAsync<List<SecurityPolicy>>(cacheKey, ct).ConfigureAwait(false);

        var tenantId = Guid.Parse(request.Attributes.GetValueOrDefault("tenant_id") ?? Guid.Empty.ToString());

        if (policies is null)
        {
            policies = (await policyRepository.GetActiveByTenantAsync(new TenantId(tenantId), ct)
                .ConfigureAwait(false)).ToList();

            await cacheService.SetAsync(cacheKey, policies, TimeSpan.FromMinutes(1), ct)
                .ConfigureAwait(false);
        }

        // Build minimal trace for evaluation
        var traceResult = Trace.Create(
            new TenantId(tenantId),
            request.AgentId,
            null,
            request.OperationName,
            request.OperationType,
            dateTimeProvider.UtcNow.AddSeconds(-1),
            dateTimeProvider.UtcNow,
            TraceStatus.Ok,
            request.Attributes,
            [],
            [],
            new TokenUsage(request.TokenUsageTotal, 0),
            new Cost(request.CostUsd));

        if (traceResult.IsFailure)
        {
            return Result<Unit>.Failure(traceResult.Error!);
        }

        var trace = traceResult.Value!;

        var violations = new List<PolicyViolatedEvent>();
        foreach (var policy in policies)
        {
            var result = policy.Evaluate(trace, dateTimeProvider);
            if (result.IsSuccess && policy.DomainEvents.Any())
            {
                var violationEvent = policy.DomainEvents
                    .OfType<PolicyViolatedEvent>()
                    .FirstOrDefault();

                if (violationEvent is not null)
                {
                    violations.Add(violationEvent);
                    await eventBus.PublishAsync(violationEvent, ct).ConfigureAwait(false);
                }
                policy.ClearDomainEvents();
            }
        }

        if (violations.Count > 0)
        {
            LogViolationsDetected(logger, request.TraceId.ToString(), violations.Count);
        }

        return Result.Success(Unit.Value);
    }
}
