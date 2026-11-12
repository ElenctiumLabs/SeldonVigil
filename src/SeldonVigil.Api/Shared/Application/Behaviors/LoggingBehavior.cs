using MediatR;

namespace SeldonVigil.Api.Shared.Application.Behaviors;

internal sealed partial class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Handling {RequestName}")]
    private static partial void LogHandlingRequest(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {RequestName} successfully")]
    private static partial void LogHandledRequest(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error handling {RequestName}")]
    private static partial void LogRequestError(ILogger logger, Exception exception, string requestName);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        LogHandlingRequest(logger, requestName);

        try
        {
            var response = await next().ConfigureAwait(false);
            LogHandledRequest(logger, requestName);
            return response;
        }
        catch (Exception ex)
        {
            LogRequestError(logger, ex, requestName);
            throw;
        }
    }
}