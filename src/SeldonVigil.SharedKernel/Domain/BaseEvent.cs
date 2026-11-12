namespace SeldonVigil.SharedKernel.Domain;

public abstract record BaseEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
}
