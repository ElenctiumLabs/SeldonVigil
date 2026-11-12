using SeldonVigil.Api.Modules.Observability.Domain.Events;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Workers.Workers;

internal sealed partial class BrokerSubscriptionBootstrapper(
    IEventBus eventBus,
    ILogger<BrokerSubscriptionBootstrapper> logger)
    : BackgroundService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Bootstrapping broker subscriptions")]
    private static partial void LogBootstrappingSubscriptions(ILogger logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogBootstrappingSubscriptions(logger);

        await eventBus.SubscribeAsync<TraceEnrichedEvent, TraceEnrichedEventHandler>(stoppingToken)
            .ConfigureAwait(false);

        // Keep the host alive; subscriptions run on their own background consumers.
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
    }
}