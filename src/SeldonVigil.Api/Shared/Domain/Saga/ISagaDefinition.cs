namespace SeldonVigil.Shared.Domain.Saga;

/// <summary>
/// Describes a saga: its type identifier, the data type it operates on, and its ordered steps.
/// </summary>
public interface ISagaDefinition
{
    /// <summary>
    /// Gets the saga type identifier.
    /// </summary>
    string SagaType { get; }

    /// <summary>
    /// Gets the type of the saga data.
    /// </summary>
    Type SagaDataType { get; }

    /// <summary>
    /// Gets the ordered steps that make up the saga.
    /// </summary>
    IReadOnlyList<ISagaStep> Steps { get; }
}
