namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record ToolCallRecord(
    string Id,
    string ToolName,
    string Arguments,
    string Result,
    long ElapsedMs);
