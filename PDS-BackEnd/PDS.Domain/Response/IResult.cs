namespace PDS.Domain.Response;

/// <summary>
/// Represents the result of an operation.
/// </summary>
public interface IResult
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// The error message if the operation was not successful.
    /// </summary>
    public string Error { get; }
}
