namespace SeldonVigil.SharedKernel.Infrastructure.Messaging;

/// <summary>
/// Classifies broker topics as dead-letter queues. The platform's configured
/// suffix is <c>.dlq</c> (see <see cref="BrokerConsumerOptions.DlqSuffix"/>), but
/// the legacy dash convention (<c>-dlq</c>) is still matched so externally
/// provisioned topics are classified correctly.
/// </summary>
public static class DlqTopicFilter
{
    /// <summary>
    /// Returns true when the topic name ends with the configured DLQ suffix or
    /// one of the well-known conventions (<c>.dlq</c>, <c>-dlq</c>), case-insensitively.
    /// </summary>
    public static bool IsDlq(string topic, string? configuredSuffix = BrokerConsumerOptions.DefaultDlqSuffix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        return Matches(topic, configuredSuffix)
            || Matches(topic, ".dlq")
            || Matches(topic, "-dlq");
    }

    private static bool Matches(string topic, string? suffix) =>
        !string.IsNullOrWhiteSpace(suffix) &&
        topic.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
}
