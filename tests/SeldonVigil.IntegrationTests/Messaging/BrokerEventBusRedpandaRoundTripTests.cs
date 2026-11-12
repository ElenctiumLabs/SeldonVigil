using System.Collections.Concurrent;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeldonVigil.SharedKernel.Domain;
using SeldonVigil.SharedKernel.Infrastructure;
using SeldonVigil.SharedKernel.Infrastructure.Messaging;
using Testcontainers.Redpanda;
using Xunit;

namespace SeldonVigil.IntegrationTests.Messaging;

public sealed record RedpandaRoundTripEvent(string Payload) : BaseEvent;

public sealed class CapturingRoundTripHandler : IEventHandler<RedpandaRoundTripEvent>
{
    public ConcurrentQueue<RedpandaRoundTripEvent> Received { get; } = new();

    public Task HandleAsync(RedpandaRoundTripEvent @event, CancellationToken ct = default)
    {
        Received.Enqueue(@event);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Proves the BrokerEventBus round-trip, DLQ forwarding, and AdminClient metadata
/// reads against a Redpanda container — the same broker surface the API, workers,
/// and BrokerEndpoints exercise in production. Runs when Docker is available.
/// </summary>
public sealed class BrokerEventBusRedpandaRoundTripTests : IAsyncLifetime
{
    // Pin the same major image line used by infrastructure/docker/docker-compose.yml.
    private static readonly string Image = "docker.redpanda.com/redpandadata/redpanda:v26.2.2";

    private const string RawPoisonValue = "this is {{ definitely not json";

    private readonly RedpandaContainer _container = new RedpandaBuilder(Image).Build();

    private ServiceProvider? _services;
    private IProducer<string, string>? _producer;
    private CancellationTokenSource? _cts;
    private string _bootstrap = string.Empty;
    private string _consumerGroup = string.Empty;

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        _cts?.Cancel();
        await Task.Delay(250);
        _cts?.Dispose();
        _producer?.Dispose();
        _producer = null;

        if (_services is not null)
        {
            await _services.DisposeAsync();
            _services = null;
        }

        await _container.DisposeAsync();
    }

    [Fact]
    public async Task PublishDlqAndMetadataRoundTrip_AgainstRedpanda()
    {
        // -- Arrange ----------------------------------------------------------
        // The Redpanda module advertises its OUTSIDE listener as
        // {Hostname}:{mapped 9092}; GetBootstrapAddress() returns a URI form that
        // librdBroker cannot use, so derive the host:port directly (same approach
        // the Testcontainers Broker module uses for BootstrapServers).
        _bootstrap = $"{_container.Hostname}:{_container.GetMappedPublicPort(RedpandaBuilder.RedpandaPort)}";
        _consumerGroup = $"seldonvigil-integration-test-{Guid.NewGuid():N}";

        await WaitForBrokerAsync(_bootstrap);

        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _bootstrap,
            ClientId = "seldonvigil-integration-test"
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<CapturingRoundTripHandler>();
        _services = services.BuildServiceProvider();

        var consumerFactory = new BrokerConsumerFactory(_bootstrap);
        var options = Options.Create(new BrokerConsumerOptions { ConsumerGroup = _consumerGroup });
        var handler = _services.GetRequiredService<CapturingRoundTripHandler>();
        var bus = new EventBus(
            _producer,
            consumerFactory,
            _services.GetRequiredService<IServiceScopeFactory>(),
            options,
            _services.GetRequiredService<ILogger<EventBus>>());

        _cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var topic = nameof(RedpandaRoundTripEvent);
        var dlqTopic = topic + BrokerConsumerOptions.DefaultDlqSuffix;

        // --- Act: subscribe, publish a valid event, expect the handler to see it --
        _ = bus.SubscribeAsync<RedpandaRoundTripEvent, CapturingRoundTripHandler>(_cts.Token);

        var expected = new RedpandaRoundTripEvent($"payload-{Guid.NewGuid():N}");
        await bus.PublishAsync(expected, _cts.Token);

        await WaitUntilAsync(
            () => handler.Received.Any(e => e.EventId == expected.EventId),
            TimeSpan.FromSeconds(60),
            $"handler to receive {topic}");

        handler.Received.Should().ContainSingle(e => e.EventId == expected.EventId)
            .Which.Payload.Should().Be(expected.Payload);

        // --- Act: produce a poison message, expect it forwarded to the DLQ -------
        _ = await _producer.ProduceAsync(
            topic,
            new Message<string, string> { Key = "poison", Value = RawPoisonValue },
            _cts.Token);

        var landedOnDlq = await ConsumeUntilAsync(
            _bootstrap,
            dlqTopic,
            value => value.Contains(RawPoisonValue),
            TimeSpan.FromSeconds(60));

        landedOnDlq.Should().BeTrue("the poison payload must be forwarded to {DlqTopic}");

        // --- Act: read topic + consumer-group metadata like BrokerEndpoints --------
        using var adminClient = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = _bootstrap }).Build();

        var topics = await WaitUntilAsync(
            () =>
            {
                var meta = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
                return meta.Topics.Where(t => !t.Topic.StartsWith("__", StringComparison.Ordinal))
                    .Select(t => t.Topic)
                    .ToList();
            },
            list => list.Contains(topic) && list.Contains(dlqTopic),
            TimeSpan.FromSeconds(30),
            $"{topic} and {dlqTopic} to appear in metadata");

        topics.Should().Contain(topic);
        topics.Should().Contain(dlqTopic);

        await WaitUntilAsync(
            async () =>
            {
                var groups = await adminClient.ListConsumerGroupsAsync(
                    new ListConsumerGroupsOptions { RequestTimeout = TimeSpan.FromSeconds(5) });
                return groups.Valid.Any(g => g.GroupId == _consumerGroup);
            },
            TimeSpan.FromSeconds(30),
            $"consumer group {_consumerGroup} to be listed");
    }

    private static async Task WaitForBrokerAsync(string bootstrap)
    {
        await WaitUntilAsync(
            async () =>
            {
                try
                {
                    using var admin = new AdminClientBuilder(
                        new AdminClientConfig { BootstrapServers = bootstrap }).Build();
                    _ = admin.GetMetadata(TimeSpan.FromSeconds(5));
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            },
            TimeSpan.FromSeconds(90),
            "broker to become reachable");
    }

    private static async Task<bool> ConsumeUntilAsync(
        string bootstrap,
        string topic,
        Func<string, bool> predicate,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (ConsumeSnapshot(bootstrap, topic, predicate))
            {
                return true;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Timed out waiting for a message matching the predicate on {topic}.");
    }

    private static bool ConsumeSnapshot(string bootstrap, string topic, Func<string, bool> predicate)
    {
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = $"seldonvigil-integration-test-dlq-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();

        consumer.Subscribe(topic);
        try
        {
            for (var messageCount = 0; messageCount < 20; messageCount++)
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(2));
                if (result?.Message != null && predicate(result.Message.Value))
                {
                    return true;
                }
            }
        }
        finally
        {
            consumer.Close();
        }

        return false;
    }

    private static Task WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        string description)
    {
        return WaitUntilCoreAsync(() => condition(), timeout, description);
    }

    private static async Task<T> WaitUntilAsync<T>(
        Func<T> supplier,
        Func<T, bool> predicate,
        TimeSpan timeout,
        string description)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            var value = supplier();
            if (predicate(value))
            {
                return value;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Timed out waiting for {description}.");
            }

            await Task.Delay(200);
        }
    }

    private static Task WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout,
        string description)
    {
        return WaitUntilCoreAsync(() => Task.FromResult(condition()), timeout, description);
    }

    private static async Task WaitUntilCoreAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        string description)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!await condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Timed out waiting for {description}.");
            }

            await Task.Delay(200);
        }
    }
}
