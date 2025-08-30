namespace ByteBook.Application.Common;

public class Result
{
    public bool IsSuccess { get; protected set; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorMessage { get; protected set; }
    public Dictionary<string, string[]>? ValidationErrors { get; protected set; }

    protected Result(bool isSuccess, string? errorMessage = null, Dictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors;
    }

    public static Result Success() => new(true);
    public static Result Failure(string errorMessage) => new(false, errorMessage);
    public static Result ValidationFailure(Dictionary<string, string[]> validationErrors) => new(false, "Validation failed", validationErrors);
    public static Result ValidationFailure(string field, string error) => new(false, "Validation failed", new Dictionary<string, string[]> { { field, new[] { error } } });
}

public class Result<T> : Result
{
    public T? Value { get; private set; }

    private Result(bool isSuccess, T? value = default, string? errorMessage = null, Dictionary<string, string[]>? validationErrors = null)
        : base(isSuccess, errorMessage, validationErrors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value);
    public static new Result<T> Failure(string errorMessage) => new(false, default, errorMessage);
    public static new Result<T> ValidationFailure(Dictionary<string, string[]> validationErrors) => new(false, default, "Validation failed", validationErrors);
    public static new Result<T> ValidationFailure(string field, string error) => new(false, default, "Validation failed", new Dictionary<string, string[]> { { field, new[] { error } } });

    public static implicit operator Result<T>(T value) => Success(value);
}

public static class ResultExtensions
{
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper)
    {
        if (result.IsFailure)
            return Result<TOut>.Failure(result.ErrorMessage ?? "Unknown error");

        return Result<TOut>.Success(mapper(result.Value!));
    }

    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<TOut>> mapper)
    {
        if (result.IsFailure)
            return Result<TOut>.Failure(result.ErrorMessage ?? "Unknown error");

        var mappedValue = await mapper(result.Value!);
        return Result<TOut>.Success(mappedValue);
    }

    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
    {
        if (result.IsFailure)
            return result;

        if (!predicate(result.Value!))
            return Result<T>.Failure(errorMessage);

        return result;
    }

    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> binder)
    {
        if (result.IsFailure)
            return Result<TOut>.Failure(result.ErrorMessage ?? "Unknown error");

        return binder(result.Value!);
    }

    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> binder)
    {
        if (result.IsFailure)
            return Result<TOut>.Failure(result.ErrorMessage ?? "Unknown error");

        return await binder(result.Value!);
    }
}