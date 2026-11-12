using Confluent.Kafka;

namespace SeldonVigil.SharedKernel.Infrastructure.Messaging;

public sealed class BrokerConsumerFactory : IBrokerConsumerFactory
{
    private readonly string _bootstrapServers;
    private readonly BrokerSecurityOptions? _security;

    public BrokerConsumerFactory(string bootstrapServers, BrokerSecurityOptions? security = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bootstrapServers);
        _bootstrapServers = bootstrapServers;
        _security = security;
    }

    public IConsumer<string, TValue> CreateConsumer<TValue>(
        string topic,
        string consumerGroup,
        IDeserializer<TValue> valueDeserializer)
    {
        var config = BrokerClientSecurity.Apply(new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = consumerGroup,
            ClientId = $"seldonvigil-{topic}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            AllowAutoCreateTopics = true
        }, _security);

        return new ConsumerBuilder<string, TValue>(config)
            .SetValueDeserializer(valueDeserializer)
            .Build();
    }
}