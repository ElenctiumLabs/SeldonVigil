using SeldonVigil.Shared.Domain.Saga;

namespace SeldonVigil.Shared.Infrastructure.Saga;

/// <summary>
/// Default saga registry backed by the registered saga definitions.
/// </summary>
internal sealed class SagaRegistry(IEnumerable<ISagaDefinition> definitions) : ISagaRegistry
{
    private readonly IReadOnlyDictionary<string, ISagaDefinition> _definitions =
        definitions.ToDictionary(d => d.SagaType, StringComparer.Ordinal);

    public ISagaDefinition? GetDefinition(string sagaType) =>
        _definitions.TryGetValue(sagaType, out var definition) ? definition : null;
}
