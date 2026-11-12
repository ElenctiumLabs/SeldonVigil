namespace SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

internal sealed record RateLimit
{
    public int RequestsPerMinute { get; init; } = 60;
    public int RequestsPerHour { get; init; } = 1000;
    public int BurstLimit { get; init; } = 10;
}
