namespace SeldonVigil.Api.Modules.Governance.Domain.ValueObjects;

internal readonly record struct CertificationPipelineId(Guid Value)
{
    public static CertificationPipelineId New() => new(Guid.NewGuid());
    public static CertificationPipelineId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}
