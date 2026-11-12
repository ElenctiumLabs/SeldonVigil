namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record ToolResult
{
    public bool Success { get; init; }
    public object? Data { get; init; }
    public string? Error { get; init; }
    public long ExecutionTimeMs { get; init; }
}
