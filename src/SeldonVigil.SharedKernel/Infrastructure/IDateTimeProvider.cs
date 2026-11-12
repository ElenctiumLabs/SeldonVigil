namespace SeldonVigil.SharedKernel.Infrastructure;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTimeOffset UtcNowOffset { get; }
}
