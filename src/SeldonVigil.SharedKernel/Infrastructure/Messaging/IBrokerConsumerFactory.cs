using Confluent.Kafka;

namespace SeldonVigil.SharedKernel.Infrastructure.Messaging;

public interface IBrokerConsumerFactory
{
    IConsumer<string, TValue> CreateConsumer<TValue>(
        string topic,
        string consumerGroup,
        IDeserializer<TValue> valueDeserializer);
}
