namespace SeldonVigil.SharedKernel.Domain;

public abstract record BaseValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }
        return hash.ToHashCode();
    }

    public virtual bool Equals(BaseValueObject? other)
    {
        if (other is null)
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }
}
