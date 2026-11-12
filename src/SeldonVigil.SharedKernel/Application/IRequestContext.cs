namespace SeldonVigil.SharedKernel.Application;

public interface IRequestContext
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    IReadOnlyDictionary<string, string> Claims { get; }
}
