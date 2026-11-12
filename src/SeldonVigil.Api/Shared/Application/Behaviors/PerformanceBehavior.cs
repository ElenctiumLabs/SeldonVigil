using System.Diagnostics;
using MediatR;

namespace SeldonVigil.Api.Shared.Application.Behaviors;

internal sealed partial class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const long SlowRequestThresholdMs = 500;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Slow request detected: {RequestName} took {Elapsed}ms")]
    private static partial void LogSlowRequest(ILogger logger, string requestName, long elapsed);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next().ConfigureAwait(false);
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            LogSlowRequest(logger, requestName, stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}