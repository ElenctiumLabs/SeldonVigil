namespace SeldonVigil.SharedKernel.Domain;

public abstract class BaseAggregateRoot<TId> : BaseEntity<TId> where TId : struct
{
    protected BaseAggregateRoot() { }

    protected BaseAggregateRoot(TId id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public int Version { get; protected set; }

    public void IncrementVersion() => Version++;
}
