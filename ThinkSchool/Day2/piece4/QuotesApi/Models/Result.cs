namespace QuotesApi.Models;

public class Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess => Error == null;

    private Result(T value)
    {
        Value = value;
    }

    private Result(string error)
    {
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
}
