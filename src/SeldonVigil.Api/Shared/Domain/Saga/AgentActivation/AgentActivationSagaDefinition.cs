using SeldonVigil.Shared.Domain.Saga;

namespace SeldonVigil.Shared.Domain.Saga.AgentActivation;

/// <summary>
/// Definition of the agent activation saga.
/// </summary>
internal sealed class AgentActivationSagaDefinition(IEnumerable<ISagaStep> steps) : ISagaDefinition
{
    public string SagaType => "AgentActivation";

    public Type SagaDataType => typeof(AgentActivationSagaData);

    public IReadOnlyList<ISagaStep> Steps { get; } = steps
        .Where(s => s.Name == "ApplySecurityPolicies")
        .OrderBy(s => s.Order)
        .ToList();
}
