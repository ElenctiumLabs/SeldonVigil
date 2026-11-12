using OpenTelemetry;
using System.Diagnostics;
using SeldonVigil.Sdk.Observability;

namespace SeldonVigil.Sdk.OpenTelemetry;

/// <summary>
/// OpenTelemetry exporter for SeldonVigil traces.
/// </summary>
public class SeldonVigilTraceExporter : BaseExporter<Activity>
{
    private readonly ObservabilityClient _observabilityClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeldonVigilTraceExporter"/> class.
    /// </summary>
    /// <param name="observabilityClient">The observability client.</param>
    public SeldonVigilTraceExporter(ObservabilityClient observabilityClient)
    {
        _observabilityClient = observabilityClient ?? throw new ArgumentNullException(nameof(observabilityClient));
    }

    /// <summary>
    /// Exports a batch of activities to SeldonVigil.
    /// </summary>
    /// <remarks>
    /// Delivery is synchronous and per-activity; any failure returns
    /// <see cref="ExportResult.Failure"/> so the OpenTelemetry SDK re-exports the batch
    /// according to its own retry policy. (Previously each activity was handed to a
    /// fire-and-forget Task.Run and the batch always reported Success - silent loss.)
    /// </remarks>
    public override ExportResult Export(in Batch<Activity> batch)
    {
        var allSucceeded = true;

        foreach (var activity in batch)
        {
            if (activity == null) continue;

            try
            {
                _observabilityClient
                    .IngestTraceAsync(ConvertActivityToTrace(activity))
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex) when (ex is HttpRequestException
                                          or TaskCanceledException
                                          or InvalidOperationException
                                          or TimeoutException)
            {
                // Reported to OpenTelemetry via ExportResult.Failure - its internal
                // event source surfaces export failures.
                _ = ex;
                allSucceeded = false;
            }
        }

        return allSucceeded ? ExportResult.Success : ExportResult.Failure;
    }

    private static SeldonVigilTrace ConvertActivityToTrace(Activity activity)
    {
        return new SeldonVigilTrace
        {
            Id = activity.TraceId.ToString(),
            OperationName = activity.DisplayName,
            OperationType = activity.Kind.ToString(),
            Status = ConvertStatus(activity.Status),
            StartedAt = activity.StartTimeUtc,
            EndedAt = activity.StartTimeUtc.Add(activity.Duration),
            DurationMs = (long)activity.Duration.TotalMilliseconds,
            Attributes = activity.Tags.ToDictionary(t => t.Key, t => (object)t.Value!),
            Events = activity.Events.Select(e => new TraceEvent
            {
                Name = e.Name,
                Timestamp = e.Timestamp.UtcDateTime,
                Attributes = e.Tags.ToDictionary(t => t.Key, t => t.Value!)
            }).ToList()
        };
    }

    private static TraceStatus ConvertStatus(ActivityStatusCode? status)
    {
        if (!status.HasValue) return TraceStatus.Ok;

        return status.Value switch
        {
            ActivityStatusCode.Unset => TraceStatus.Ok,
            ActivityStatusCode.Ok => TraceStatus.Ok,
            ActivityStatusCode.Error => TraceStatus.Error,
            _ => TraceStatus.Ok
        };
    }
}
