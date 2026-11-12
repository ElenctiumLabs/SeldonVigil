namespace SeldonVigil.Api.Shared.Domain;

internal interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
