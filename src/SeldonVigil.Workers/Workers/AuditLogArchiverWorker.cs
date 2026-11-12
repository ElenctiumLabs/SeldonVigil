namespace SeldonVigil.Workers.Workers;

internal sealed partial class AuditLogArchiverWorker(
    ILogger<AuditLogArchiverWorker> logger)
    : BackgroundService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Audit Log Archiver Worker started")]
    private static partial void LogWorkerStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in audit log archiving loop")]
    private static partial void LogArchiveLoopError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Audit log archival completed")]
    private static partial void LogArchiveCompleted(ILogger logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Archive old audit logs
                await ArchiveAuditLogsAsync().ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogArchiveLoopError(logger, ex);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ArchiveAuditLogsAsync()
    {
        // Implementation would archive logs older than retention period
        await Task.CompletedTask.ConfigureAwait(false);
        LogArchiveCompleted(logger);
    }
}
