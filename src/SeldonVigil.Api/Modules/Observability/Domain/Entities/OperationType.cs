namespace SeldonVigil.Api.Modules.Observability.Domain.Entities;

public enum OperationType
{
    LlmCall,
    ToolCall,
    AgentHandoff,
    Custom
}
