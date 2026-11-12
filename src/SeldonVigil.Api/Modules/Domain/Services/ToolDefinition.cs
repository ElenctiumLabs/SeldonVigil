using System.Text.Json;

namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record ToolDefinition(
    string Name,
    string Description,
    Dictionary<string, string> Parameters,
    Func<Dictionary<string, JsonElement>, IServiceProvider, CancellationToken, Task<object?>> Handler);
