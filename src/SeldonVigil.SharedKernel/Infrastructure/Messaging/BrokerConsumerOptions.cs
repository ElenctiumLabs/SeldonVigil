namespace SeldonVigil.SharedKernel.Infrastructure.Messaging;

public sealed class BrokerConsumerOptions
{
    public const string SectionName = "Broker";

    public const string DefaultDlqSuffix = ".dlq";

    public string ConsumerGroup { get; set; } = "seldonvigil";

    public string DlqSuffix { get; set; } = DefaultDlqSuffix;
}
