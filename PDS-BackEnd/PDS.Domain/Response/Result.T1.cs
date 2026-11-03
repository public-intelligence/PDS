namespace PDS.Domain.Response;

/// <summary>
/// Represents the result of an operation.
/// </summary>
public class Result<T> : Result, IResult<T>
{
    private readonly T? _value;

    /// <summary>
    /// The value of the result.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when IsSuccess is false.</exception>
    /// <returns>The value of the result.</returns>
    public T Value =>
        _isSuccess
            ? _value!
            : throw new InvalidOperationException(
                "Value can not be accessed when IsSuccess is false."
            );

    private Result(T value)
        : base()
    {
        _value = value;
    }

    private Result(string error)
        : base(error) { }

    /// <summary>
    /// Creates a new successful result.
    /// </summary>
    /// <param name="value">The value of the result.</param>
    /// <returns>A new successful result.</returns>
    public static Result<T> Success(T value)
    {
        var result = new Result<T>(value);
        return result;
    }

    /// <summary>
    /// Creates a new failure result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A new failure result.</returns>
    public static new Result<T> Failure(string error)
    {
        var result = new Result<T>(error);
        return result;
    }
}
