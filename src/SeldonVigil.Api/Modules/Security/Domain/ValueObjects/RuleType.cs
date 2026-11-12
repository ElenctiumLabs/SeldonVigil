namespace SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

public enum RuleType
{
    RegexMatch,
    Threshold,
    Behavioral,
    Composite
}
