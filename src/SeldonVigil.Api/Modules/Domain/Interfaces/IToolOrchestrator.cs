using System.Collections.ObjectModel;
using SeldonVigil.Api.Modules.Domain.Services;

namespace SeldonVigil.Api.Modules.Domain.Interfaces;

internal interface IToolOrchestrator
{
    ReadOnlyCollection<AgentTool> GetAvailableTools();
    Task<string> ExecuteAsync(string toolName, string argumentsJson, CancellationToken ct = default);
}
