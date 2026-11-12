namespace SeldonVigil.Api.Modules.CostIntelligence.Api.Endpoints;

internal sealed record CostDashboardRequest(
    DateTime? From,
    DateTime? To,
    string? GroupBy);
