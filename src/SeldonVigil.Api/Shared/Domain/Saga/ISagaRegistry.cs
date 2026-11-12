namespace SeldonVigil.Shared.Domain.Saga;

/// <summary>
/// Resolves the definition for a saga type.
/// </summary>
public interface ISagaRegistry
{
    /// <summary>
    /// Gets the definition for the given saga type, or null if none is registered.
    /// </summary>
    /// <param name="sagaType">The saga type identifier.</param>
    /// <returns>The saga definition, or null.</returns>
    ISagaDefinition? GetDefinition(string sagaType);
}
