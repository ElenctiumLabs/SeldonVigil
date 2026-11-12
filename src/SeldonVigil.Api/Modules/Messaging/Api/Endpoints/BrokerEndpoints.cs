

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;
using SeldonVigil.SharedKernel.Infrastructure.Messaging;

namespace SeldonVigil.Api.Modules.Messaging.Api.Endpoints;

/// <summary>
/// Provides read-only broker management endpoints consumed by the SeldonVigil frontend UI.
/// Uses IAdminClient for topic/group metadata; no MediatR required for these thin queries.
/// </summary>
internal static class BrokerEndpoints
{
    // Server-side cap on payload size returned per message by the viewer endpoint.
    private const int MaxMessageValueLength = 16_000;

    // Upper bound on how many messages a single viewer request may return.
    private const int MaxMessageLimit = 200;

    // Overall deadline for consuming messages across all partitions of a topic.
    private static readonly TimeSpan MessageConsumeDeadline = TimeSpan.FromSeconds(5);
    public static IEndpointRouteBuilder MapBrokerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/broker")
            .WithTags("Broker")

            .RequireAuthorization();

        // GET /api/v1/broker/status
        group.MapGet("/status", async (
            IConfiguration config,
            IOptions<BrokerConsumerOptions> brokerOpts,
            IOptions<BrokerSecurityOptions> securityOpts,
            CancellationToken ct) =>
        {
            var bootstrapServer = BrokerBootstrapServers.Normalize(config.GetConnectionString("Broker") ?? "localhost:9092");

            using var adminClient = new AdminClientBuilder(
                BrokerClientSecurity.Apply(new AdminClientConfig { BootstrapServers = bootstrapServer }, securityOpts.Value))
                .Build();

            try
            {
                var meta = adminClient.GetMetadata(TimeSpan.FromSeconds(5));

                var topics = meta.Topics
                    .Where(t => !t.Topic.StartsWith("__", StringComparison.Ordinal))
                    .ToList();

                var partitionCount = topics.Sum(t => t.Partitions.Count);

                // Consumer groups require a separate describe call
                int groupCount;
                try
                {
                    var groups = await adminClient.ListConsumerGroupsAsync(
                        new ListConsumerGroupsOptions { RequestTimeout = TimeSpan.FromSeconds(5) })
                        .ConfigureAwait(false);
                    groupCount = groups.Valid.Count;
                }
                catch (Exception)
                {
                    groupCount = 0;
                }

                return Results.Ok(new BrokerStatusDto(
                    BootstrapServer: bootstrapServer,
                    NodeId: meta.Brokers.FirstOrDefault()?.BrokerId ?? 1,
                    ClusterId: $"redpanda-{meta.OriginatingBrokerId}",
                    IsReachable: true,
                    TopicCount: topics.Count,
                    PartitionCount: partitionCount,
                    ConsumerGroupCount: groupCount,
                    Version: "Redpanda",
                    UptimeSeconds: 0));
            }
            catch (Exception)
            {
                return Results.Ok(new BrokerStatusDto(
                    BootstrapServer: bootstrapServer,
                    NodeId: 1,
                    ClusterId: "unreachable",
                    IsReachable: false,
                    TopicCount: 0,
                    PartitionCount: 0,
                    ConsumerGroupCount: 0,
                    Version: "Redpanda",
                    UptimeSeconds: 0));
            }
        })
        .WithName("GetBrokerStatus")
        .WithSummary("Get broker health and metadata")
        .Produces<BrokerStatusDto>()
        ; // Auth enforced - previously AllowAnonymous (info disclosure)

        // GET /api/v1/broker/topics
        _ = group.MapGet("/topics", (
            IConfiguration config,
            IOptions<BrokerConsumerOptions> brokerOpts,
            IOptions<BrokerSecurityOptions> securityOpts,
            CancellationToken ct) =>
        {
            try
            {
                var bootstrapServer = BrokerBootstrapServers.Normalize(config.GetConnectionString("Broker") ?? "localhost:9092");

                try
                {
                    using var adminClient = new AdminClientBuilder(
                            BrokerClientSecurity.Apply(new AdminClientConfig { BootstrapServers = bootstrapServer }, securityOpts.Value))
                        .Build();

                    var meta = WithBrokerRetry(
                        () => adminClient.GetMetadata(TimeSpan.FromSeconds(5)), ct);

                    var topics = meta.Topics
                        .Where(t => !t.Topic.StartsWith("__", StringComparison.Ordinal))
                        .OrderBy(t => t.Topic)
                        .ToList();

                    // Ephemeral consumer used purely for watermark queries; it never
                    // subscribes or joins a group, so existing consumer groups are untouched.
                    using var watermarkConsumer = new ConsumerBuilder<string, string>(BrokerClientSecurity.Apply(new ConsumerConfig
                    {
                        BootstrapServers = bootstrapServer,
                        GroupId = "seldonvigil-metadata-queries",
                        EnableAutoCommit = false,
                        AllowAutoCreateTopics = false,
                    }, securityOpts.Value)).Build();

                    var result = new List<BrokerTopicDto>(topics.Count);

                    foreach (var t in topics)
                    {
                        ct.ThrowIfCancellationRequested();

                        // Approximate live message count from per-partition watermarks.
                        long messageCount = 0;
                        foreach (var p in t.Partitions)
                        {
                            try
                            {
                                var wm = WithBrokerRetry(
                                    () => watermarkConsumer.QueryWatermarkOffsets(
                                        new TopicPartition(t.Topic, p.PartitionId), TimeSpan.FromSeconds(3)),
                                    ct);
                                messageCount += Math.Max(0, (long)wm.High - (long)wm.Low);
                            }
                            catch
                            {
                                // A single unreachable partition must not break the topic list.
                            }
                        }

                        var isDlq = DlqTopicFilter.IsDlq(t.Topic, brokerOpts.Value.DlqSuffix);

                        result.Add(new BrokerTopicDto(
                            Name: t.Topic,
                            Partitions: t.Partitions.Count,
                            ReplicationFactor: t.Partitions.FirstOrDefault()?.Replicas.Length ?? 1,
                            RetentionMs: 86400000,   // Default; real value needs DescribeConfigs
                            CompressionType: isDlq ? "none" : "lz4",
                            MessageCount: messageCount,
                            SizeBytes: 0,
                            IsDlq: isDlq));
                    }

                    return Task.FromResult(Results.Ok(result));
                }
                catch
                {
                    return Task.FromResult(Results.Ok(Array.Empty<BrokerTopicDto>()));
                }
            }
            catch (Exception exception)
            {
                return Task.FromException<IResult>(exception);
            }
        })
        .WithName("ListBrokerTopics")
        .WithSummary("List all broker topics with approximate message counts")
        .Produces<IReadOnlyList<BrokerTopicDto>>()
        ; // Auth enforced - previously AllowAnonymous (info disclosure)

        // GET /api/v1/broker/topics/{topicName}/messages?limit=20
        // Read-only tail viewer: assigns each partition at high-watermark minus the
        // limit and consumes without committing, so consumer groups are untouched.
        group.MapGet("/topics/{topicName}/messages", async (
            string topicName,
            IConfiguration config,
            IOptions<BrokerSecurityOptions> securityOpts,
            CancellationToken ct,
            int limit = 20) =>
        {
            if (limit < 1) limit = 20;
            limit = Math.Min(limit, MaxMessageLimit);

            var bootstrapServer = BrokerBootstrapServers.Normalize(config.GetConnectionString("Broker") ?? "localhost:9092");

            try
            {
                using var adminClient = new AdminClientBuilder(
                    BrokerClientSecurity.Apply(new AdminClientConfig { BootstrapServers = bootstrapServer }, securityOpts.Value))
                    .Build();

                // Full-cluster metadata on purpose: requesting metadata for a specific
                // topic name makes brokers with auto.create.topics.enable=true create
                // it as a side effect, which would silently pollute the broker.
                Metadata metadata;
                try
                {
                    metadata = WithBrokerRetry(() => adminClient.GetMetadata(TimeSpan.FromSeconds(5)), ct);
                }
                catch (KafkaException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    return Results.NotFound(new { error = $"Topic '{topicName}' not found." });
                }

                var topicMetadata = metadata.Topics.SingleOrDefault(t => t.Topic == topicName);
                if (topicMetadata is null
                    || topicMetadata.Error.Code == ErrorCode.UnknownTopicOrPart
                    || topicMetadata.Partitions is not { Count: > 0 })
                {
                    return Results.NotFound(new { error = $"Topic '{topicName}' not found." });
                }

                // Built inline rather than via IBrokerConsumerFactory: the viewer needs a
                // unique non-committing group and EOF signalling, which would change the
                // semantics of consumers created for event processing.
                var consumerConfig = BrokerClientSecurity.Apply(new ConsumerConfig
                {
                    BootstrapServers = bootstrapServer,
                    GroupId = $"seldonvigil-viewer-{Guid.NewGuid():N}",
                    EnableAutoCommit = false,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnablePartitionEof = true,
                    AllowAutoCreateTopics = false,
                }, securityOpts.Value);

                using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
                    .SetKeyDeserializer(Deserializers.Utf8)
                    .SetValueDeserializer(Deserializers.Utf8)
                    .Build();

                // Start near the tail so the viewer shows the most recent messages.
                var assignments = new List<TopicPartitionOffset>();
                foreach (var p in topicMetadata.Partitions)
                {
                    var tp = new TopicPartition(topicName, p.PartitionId);
                    var wm = WithBrokerRetry(
                        () => consumer.QueryWatermarkOffsets(tp, TimeSpan.FromSeconds(3)), ct);
                    var low = Math.Max(0, (long)wm.Low);
                    var high = Math.Max(low, (long)wm.High);

                    if (high > low)
                    {
                        assignments.Add(new TopicPartitionOffset(tp, Math.Max(low, high - limit)));
                    }
                }

                var messages = new List<BrokerTopicMessageDto>();
                if (assignments.Count == 0)
                {
                    return Results.Ok(messages);
                }

                consumer.Assign(assignments);

                var deadline = Stopwatch.StartNew();
                var remaining = new Dictionary<int, int>();
                foreach (var a in assignments)
                {
                    remaining[a.Partition.Value] = limit;
                }

                while (remaining.Count > 0 && deadline.Elapsed < MessageConsumeDeadline && !ct.IsCancellationRequested)
                {
                    ConsumeResult<string, string>? result;
                    try
                    {
                        result = consumer.Consume(TimeSpan.FromMilliseconds(250));
                    }
                    catch (ConsumeException)
                    {
                        continue;   // Skip malformed records; keep draining other partitions.
                    }

                    if (result is null)
                    {
                        continue;
                    }

                    var partition = result.Partition.Value;

                    if (!remaining.TryGetValue(partition, out var left))
                    {
                        continue;
                    }

                    if (result.IsPartitionEOF)
                    {
                        _ = remaining.Remove(partition);
                        continue;
                    }

                    messages.Add(ToMessageDto(result));
                    if (left <= 1)
                    {
                        _ = remaining.Remove(partition);
                    }
                    else
                    {
                        remaining[partition] = left - 1;
                    }
                }

                return Results.Ok(messages
                    .OrderByDescending(m => m.TimestampUtc)
                    .ThenByDescending(m => m.Offset)
                    .Take(limit)
                    .ToList());
            }
            catch (KafkaException ex)
            {
                return Results.Problem(
                    statusCode: 503,
                    title: "Broker unreachable",
                    detail: ex.Error.Reason);
            }
        })
        .WithName("ListBrokerTopicMessages")
        .WithSummary("Read the most recent messages of a topic without committing offsets")
        .Produces<IReadOnlyList<BrokerTopicMessageDto>>()
        ; // Auth enforced - previously AllowAnonymous (info disclosure)

        // GET /api/v1/broker/consumer-groups
        group.MapGet("/consumer-groups", async (
            IConfiguration config,
            IOptions<BrokerSecurityOptions> securityOpts,
            CancellationToken ct) =>
        {
            var bootstrapServer = BrokerBootstrapServers.Normalize(config.GetConnectionString("Broker") ?? "localhost:9092");

            try
            {
                using var adminClient = new AdminClientBuilder(
                    BrokerClientSecurity.Apply(new AdminClientConfig { BootstrapServers = bootstrapServer }, securityOpts.Value))
                    .Build();

                var listing = await adminClient.ListConsumerGroupsAsync(
                    new ListConsumerGroupsOptions { RequestTimeout = TimeSpan.FromSeconds(5) })
                    .ConfigureAwait(false);

                // Filter out internal groups
                var groups = listing.Valid
                    .Where(g => !g.GroupId.StartsWith("_", StringComparison.Ordinal))
                    .Select(g => new BrokerConsumerGroupDto(
                        GroupId: g.GroupId,
                        State: MapGroupState(g.State),
                        MemberCount: 0,      // Would need DescribeConsumerGroups for member details
                        Topics: [],
                        TotalLag: 0))
                    .ToList();

                if (groups.Count == 0)
                {
                    return Results.Ok(groups);
                }

                // Ephemeral consumer for watermark queries only; never joins a group.
                using var watermarkConsumer = new ConsumerBuilder<string, string>(BrokerClientSecurity.Apply(new ConsumerConfig
                {
                    BootstrapServers = bootstrapServer,
                    GroupId = "seldonvigil-metadata-queries",
                    EnableAutoCommit = false,
                    AllowAutoCreateTopics = false,
                }, securityOpts.Value)).Build();

                // Enrich with member count via DescribeConsumerGroups and real lag via
                // committed offsets vs high watermarks.
                try
                {
                    var described = await adminClient.DescribeConsumerGroupsAsync(
                        groups.Select(g => g.GroupId),
                        new DescribeConsumerGroupsOptions { RequestTimeout = TimeSpan.FromSeconds(8) })
                        .ConfigureAwait(false);

                    // Fetch committed offsets for the partitions each group is assigned.
                    var offsetRequests = groups
                        .Select(g => (Group: g.GroupId, Partitions: described.ConsumerGroupDescriptions
                            .FirstOrDefault(d => d.GroupId == g.GroupId)?.Members
                            .SelectMany(m => m.Assignment.TopicPartitions)
                            .ToList() ?? []))
                        .Where(r => r.Partitions.Count > 0)
                        .Select(r => new ConsumerGroupTopicPartitions(r.Group, r.Partitions))
                        .ToList();

                    var committedByTopic = new Dictionary<string, List<TopicPartitionOffset>>();
                    // The admin client only accepts one group per offsets request.
                    foreach (var request in offsetRequests)
                    {
                        try
                        {
                            var offsetResult = await adminClient.ListConsumerGroupOffsetsAsync(
                                [request],
                                new ListConsumerGroupOffsetsOptions { RequestTimeout = TimeSpan.FromSeconds(5) })
                                .ConfigureAwait(false);

                            var committed = offsetResult
                                .SelectMany(r => r.Partitions)
                                .Where(tpo => tpo.Error is null || tpo.Error.Code == ErrorCode.NoError)
                                .Select(tpo => new TopicPartitionOffset(tpo.TopicPartition, tpo.Offset))
                                .ToList();

                            foreach (var tpo in committed)
                            {
                                if (!committedByTopic.TryGetValue(
                                        tpo.TopicPartition.Topic, out var list))
                                {
                                    list = [];
                                    committedByTopic[tpo.TopicPartition.Topic] = list;
                                }
                                list.Add(tpo);
                            }
                        }
                        catch
                        {
                            // Per-group offsets failures degrade to zero lag rather than hiding details.
                        }
                    }

                    var enriched = groups.Select(g =>
                    {
                        var detail = described.ConsumerGroupDescriptions
                            .FirstOrDefault(d => d.GroupId == g.GroupId);
                        if (detail is not null)
                        {
                            g = g with
                            {
                                MemberCount = detail.Members.Count,
                                Topics = detail.Members
                                    .SelectMany(m => m.Assignment.TopicPartitions.Select(tp => tp.Topic))
                                    .Distinct()
                                    .OrderBy(t => t)
                                    .ToArray(),
                                State = MapGroupState(detail.State),
                            };
                        }

                        // Committed offsets are reported per assigned topic; for the
                        // platform's single-topic consumer groups this lag is exact.
                        long totalLag = 0;
                        foreach (var topic in g.Topics)
                        {
                            if (!committedByTopic.TryGetValue(topic, out var committed))
                            {
                                continue;
                            }

                            foreach (var tpo in committed)
                            {
                                try
                                {
                                    var wm = watermarkConsumer.QueryWatermarkOffsets(
                                        tpo.TopicPartition, TimeSpan.FromSeconds(3));
                                    totalLag += Math.Max(0, wm.High.Value - tpo.Offset.Value);
                                }
                                catch
                                {
                                    // Unreachable partition must not break the listing.
                                }
                            }
                        }

                        return g with { TotalLag = totalLag };
                    }).ToList();

                    return Results.Ok(enriched);
                }
                catch
                {
                    // Return basic listing without enrichment
                }

                return Results.Ok(groups);
            }
            catch
            {
                return Results.Ok(Array.Empty<BrokerConsumerGroupDto>());
            }
        })
        .WithName("ListBrokerConsumerGroups")
        .WithSummary("List broker consumer groups with state and membership")
        .Produces<IReadOnlyList<BrokerConsumerGroupDto>>()
        ; // Auth enforced - previously AllowAnonymous (info disclosure)

        // GET /api/v1/broker/config
        group.MapGet("/config", (
            IConfiguration config,
            IOptions<BrokerConsumerOptions> brokerOpts) =>
        {
            return Results.Ok(new BrokerConfigDto(
                BootstrapServer: config.GetConnectionString("Broker") ?? "localhost:9092",
                ConsumerGroup: brokerOpts.Value.ConsumerGroup,
                DlqSuffix: brokerOpts.Value.DlqSuffix,
                HeapOpts: "none (no JVM)",
                LogDir: "container:/var/log/redpanda",
                DataDir: "container:/var/lib/redpanda/data"));
        })
        .WithName("GetBrokerConfig")
        .WithSummary("Get broker configuration values from appsettings")
        .Produces<BrokerConfigDto>()
        ; // Auth enforced - previously AllowAnonymous (info disclosure)

        return app;
    }

    private static string MapGroupState(ConsumerGroupState state) => state switch
    {
        ConsumerGroupState.Stable              => "Stable",
        ConsumerGroupState.PreparingRebalance  => "PreparingRebalance",
        ConsumerGroupState.CompletingRebalance => "CompletingRebalance",
        ConsumerGroupState.Empty               => "Empty",
        ConsumerGroupState.Dead                => "Dead",
        _                                      => "Unknown",
    };

    // A brand-new librdkafka client can fail its first metadata round trip while
    // bootstrap connections are still being established ("all brokers down");
    // retry briefly before giving up.
    private static T WithBrokerRetry<T>(Func<T> action, CancellationToken ct, int attempts = 3)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return action();
            }
            catch (KafkaException) when (attempt < attempts && !ct.IsCancellationRequested)
            {
                Thread.Sleep(400);
            }
        }
    }

    private static BrokerTopicMessageDto ToMessageDto(ConsumeResult<string, string> result)
    {
        var raw = result.Message.Value ?? string.Empty;
        var truncated = raw.Length > MaxMessageValueLength;

        Dictionary<string, string>? headers = null;
        if (result.Message.Headers is { Count: > 0 })
        {
            headers = [];
            foreach (var header in result.Message.Headers)
            {
                var bytes = header.GetValueBytes();
                if (bytes is not null)
                {
                    headers[header.Key] = Encoding.UTF8.GetString(bytes);
                }
            }
        }

        return new BrokerTopicMessageDto(
            Partition: result.Partition.Value,
            Offset: result.Offset.Value,
            Key: result.Message.Key,
            Value: truncated ? raw[..MaxMessageValueLength] : raw,
            TimestampUtc: result.Message.Timestamp.UtcDateTime,
            Headers: headers,
            Truncated: truncated);
    }
}

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

internal sealed record BrokerStatusDto(
    string BootstrapServer,
    int NodeId,
    string ClusterId,
    bool IsReachable,
    int TopicCount,
    int PartitionCount,
    int ConsumerGroupCount,
    string Version,
    long UptimeSeconds);

internal sealed record BrokerTopicDto(
    string Name,
    int Partitions,
    int ReplicationFactor,
    long RetentionMs,
    string CompressionType,
    long MessageCount,
    long SizeBytes,
    bool IsDlq);

internal sealed record BrokerConsumerGroupDto(
    string GroupId,
    string State,
    int MemberCount,
    string[] Topics,
    long TotalLag);

internal sealed record BrokerConfigDto(
    string BootstrapServer,
    string ConsumerGroup,
    string DlqSuffix,
    string HeapOpts,
    string LogDir,
    string DataDir);

internal sealed record BrokerTopicMessageDto(
    long Partition,
    long Offset,
    string? Key,
    string Value,
    DateTime TimestampUtc,
    IReadOnlyDictionary<string, string>? Headers,
    bool Truncated);
