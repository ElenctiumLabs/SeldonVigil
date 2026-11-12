using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.SharedKernel.Infrastructure;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default);
    Task SubscribeAsync<TEvent, THandler>(CancellationToken ct = default)
        where TEvent : BaseEvent
        where THandler : IEventHandler<TEvent>;
}
