namespace SeldonVigil.SharedKernel.Domain;

/// <summary>
/// Represents the outcome of an operation that can either succeed or fail.
/// Eliminates exceptions for expected failure paths.
/// </summary>
public readonly record struct Result<T>
{
    public bool IsSuccess { get; init; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; init; }
    public Error? Error { get; init; }

    public static Result<T> Success(T value) => new()
    {
        IsSuccess = true,
        Value = value
    };

    public static Result<T> Failure(Error error) => new()
    {
        IsSuccess = false,
        Error = error
    };

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);

    public void Match(Action<T> onSuccess, Action<Error> onFailure)
    {
        if (IsSuccess)
        {
            onSuccess(Value!);
        }
        else
        {
            onFailure(Error!);
        }
    }
}
