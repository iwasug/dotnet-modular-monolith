namespace ModularMonolith.Shared.Common;

/// <summary>
/// Result pattern for error handling with success/failure states
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; private init; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; private init; }
    public Error Error { get; private init; } = Error.None;

    private Result(bool isSuccess, T? value, Error error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, Error.None);
    public static Result<T> Failure(Error error) => new(false, default, error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error);
    }
}

/// <summary>
/// Result pattern without value for operations that don't return data
/// </summary>
public class Result
{
    public bool IsSuccess { get; private init; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; private init; } = Error.None;

    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error);
    }
}