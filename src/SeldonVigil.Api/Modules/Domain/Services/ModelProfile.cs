namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record ModelProfile(
    string Name,
    double InputCostPer1mTokens,
    double OutputCostPer1mTokens,
    int ContextWindow,
    double AccuracyScore,
    ModelCapability Capability)
{
    public double EstimatedCostPer1kTokens => (InputCostPer1mTokens + OutputCostPer1mTokens * 2) / 1000;
}
