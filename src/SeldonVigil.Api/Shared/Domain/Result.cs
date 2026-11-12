namespace SeldonVigil.Api.Shared.Domain;

internal class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    protected SeldonVigil.SharedKernel.Domain.Error? ErrorObject { get; }

    protected Result(bool isSuccess, string? error, SeldonVigil.SharedKernel.Domain.Error? errorObject = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorObject = errorObject;
    }

    public static Result Success() => new(true, null);
    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
    public static Result Failure(SeldonVigil.SharedKernel.Domain.Error error) => new(false, error.Message, error);
    public static Result<T> Failure<T>(SeldonVigil.SharedKernel.Domain.Error error) => new(default, false, error.Message, error);
}

internal class Result<T>(T? value, bool isSuccess, string? error, SeldonVigil.SharedKernel.Domain.Error? errorObject = null) : Result(isSuccess, error, errorObject)
{
    private T? Value { get; } = value;

    public static new Result<T> Failure(SeldonVigil.SharedKernel.Domain.Error error) => new(default, false, error.Message, error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<SeldonVigil.SharedKernel.Domain.Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(ErrorObject!);
}
