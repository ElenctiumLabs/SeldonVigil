using MediatR;

namespace SeldonVigil.Api.Shared.Domain;

internal abstract record BaseEvent : INotification
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
}
