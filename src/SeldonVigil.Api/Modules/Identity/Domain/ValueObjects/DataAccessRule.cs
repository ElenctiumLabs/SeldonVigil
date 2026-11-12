namespace SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

internal sealed record DataAccessRule
{
    public string Resource { get; init; } = string.Empty;
    public string AccessLevel { get; init; } = "read"; // read, write, delete
    public IReadOnlyList<string> AllowedFields { get; init; } = [];
}
