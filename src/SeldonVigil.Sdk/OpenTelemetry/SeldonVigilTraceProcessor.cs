using OpenTelemetry;
using System.Diagnostics;

namespace SeldonVigil.Sdk.OpenTelemetry;

/// <summary>
/// Activity processor for SeldonVigil traces.
/// </summary>
public class SeldonVigilTraceProcessor : BaseProcessor<Activity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeldonVigilTraceProcessor"/> class.
    /// </summary>
    public SeldonVigilTraceProcessor()
    {
    }

    /// <summary>
    /// Called when an activity is started.
    /// </summary>
    /// <param name="activity">The activity.</param>
    public override void OnStart(Activity activity)
    {
        // Add custom attributes or perform processing on activity start
        if (activity != null)
        {
            activity.SetTag("seldonvigil.sdk.version", "1.0.0");
        }
    }

    /// <summary>
    /// Called when an activity is ended.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <remarks>
    /// Enrichment only - trace delivery happens in <see cref="SeldonVigilTraceExporter"/>.
    /// Pushing HTTP calls from OnEnd (once per activity, unbatched, unobserved) caused
    /// silent loss and unbounded background work.
    /// </remarks>
    public override void OnEnd(Activity activity)
    {
        if (activity == null) return;
        activity.SetTag("seldonvigil.sdk.processed", true);
    }
}
