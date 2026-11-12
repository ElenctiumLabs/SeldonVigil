namespace SeldonVigil.SecurityTests;

public sealed record SecurityEventDto(
    string EventType,
    string Description,
    DateTime Timestamp,
    string? AgentId,
    string? Severity);
