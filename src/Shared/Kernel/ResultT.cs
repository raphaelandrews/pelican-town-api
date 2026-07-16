namespace PelicanTown.SharedKernel.Errors;

public sealed class Result<T> : Result
{
    public T Value { get; }

    private Result(T value) : base(true, null)
    {
        Value = value;
    }

    private Result(Error error) : base(false, error)
    {
        Value = default!;
    }

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(Error error) => new(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(Value) : onFailure(Error!);
    }

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
