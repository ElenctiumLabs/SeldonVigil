namespace SeldonVigil.Api.Shared.Domain;

internal interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<BaseEvent> events, CancellationToken cancellationToken = default);
}
