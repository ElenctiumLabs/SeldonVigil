namespace SeldonVigil.Api.Shared.Infrastructure;

internal sealed record Value
{
    public string StringValue { get; init; } = string.Empty;

    public static implicit operator Value(string value) => new() { StringValue = value };
}
