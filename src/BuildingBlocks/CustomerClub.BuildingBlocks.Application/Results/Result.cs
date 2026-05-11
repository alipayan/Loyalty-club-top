namespace CustomerClub.BuildingBlocks.Application.Results;

public class Result
{
    protected Result(bool isSuccess, Error error, IReadOnlyCollection<ValidationError>? validationErrors = null)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A successful result cannot contain an error.");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failed result must contain an error.");

        IsSuccess = isSuccess;
        Error = error;
        ValidationErrors = validationErrors ?? [];
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public IReadOnlyCollection<ValidationError> ValidationErrors { get; }

    public static Result Success()
        => new(true, Error.None);

    public static Result Failure(Error error)
        => new(false, error);

    public static Result ValidationFailure(IReadOnlyCollection<ValidationError> validationErrors)
        => new(
            false,
            Error.Validation("validation.failed", "One or more validation errors occurred."),
            validationErrors);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(true, Error.None)
    {
        _value = value;
    }

    private Result(Error error)
        : base(false, error)
    {
        _value = default;
    }

    private Result(IReadOnlyCollection<ValidationError> validationErrors)
        : base(
            false,
            Error.Validation("validation.failed", "One or more validation errors occurred."),
            validationErrors)
    {
        _value = default;
    }

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value)
        => new(value);

    public static new Result<T> Failure(Error error)
        => new(error);

    public static Result<T> ValidationFailure(IReadOnlyCollection<ValidationError> validationErrors)
        => new(validationErrors);
}