namespace SeldonVigil.Api.Modules.Security.Api.Endpoints;

internal sealed record SecurityDashboardDto(
    int TotalPolicies,
    int ActivePolicies,
    int TotalViolations24h,
    int OpenViolations,
    int CriticalViolations,
    List<RecentViolationDto> RecentViolations,
    List<TrendPointDto> ViolationTrend7d);
