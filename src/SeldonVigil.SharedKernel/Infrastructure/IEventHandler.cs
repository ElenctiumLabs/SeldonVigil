using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.SharedKernel.Infrastructure;

public interface IEventHandler<in TEvent> where TEvent : BaseEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
