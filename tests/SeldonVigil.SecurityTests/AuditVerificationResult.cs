namespace SeldonVigil.SecurityTests;

public sealed record AuditVerificationResult(
    Guid Id,
    string AggregateType,
    string AggregateId,
    bool ChecksumValid,
    bool ChainValid);
