using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDS.Domain.Response;

/// <summary>
/// Represents the result of an operation.
/// </summary>
public class Result : IResult
{
    private readonly string? _error;
    protected readonly bool _isSuccess;

    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess => _isSuccess;

    // NOTE: Is it worth to create an `Error` class? If so, should have
    // the error code (text and number) and message.

    /// <summary>
    /// The error message if the operation was not successful.
    /// </summary>
    public string Error => !_isSuccess
            ? _error!
            : throw new InvalidOperationException(
                "Error can not be accessed when IsSuccess is true."
            );

    /// <summary>
    /// Constructor for a successful result.
    /// </summary>
    protected Result()
    {
        _isSuccess = true;
    }

    /// <summary>
    /// Constructor for a failure result.
    /// </summary>
    /// <param name="error">The error message.</param>
    protected Result(string error)
    {
        _error = error;
    }

    /// <summary>
    /// Creates a new successful result.
    /// </summary>
    public static Result Success()
    {
        var result = new Result();
        return result;
    }

    /// <summary>
    /// Creates a new failure result.
    /// </summary>
    /// <param name="error">The error message.</param>
    public static Result Failure(string error)
    {
        var result = new Result(error);
        return result;
    }
}
