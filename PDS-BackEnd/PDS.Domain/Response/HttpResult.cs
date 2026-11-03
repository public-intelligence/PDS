using PDS.Domain.DTOs.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PDS.Domain.Response;

public class HttpResult : Result, IHttpResult
{
    private readonly ErrorDto? _error;

    public new ErrorDto Error => !_isSuccess
            ? _error!
            : throw new InvalidOperationException(
                "Detail can not be accessed when IsSuccess is true."
            );
    public HttpStatusCode StatusCode { get; init; }

    protected HttpResult(HttpStatusCode statusCode)
        : base()
    {
        StatusCode = statusCode;
    }

    protected HttpResult(string title, ErrorDto error, HttpStatusCode statusCode)
        : base(title)
    {
        _error = error;
        StatusCode = statusCode;
    }

    public static HttpResult Success(HttpStatusCode statusCode = HttpStatusCode.NoContent)
    {
        if (((int)statusCode) < 200 || ((int)statusCode) > 399)
            throw new ArgumentException("The status code must be a success status code.");

        var result = new HttpResult(statusCode);
        return result;
    }

    /// <summary>
    /// Creates a new failure result.
    /// </summary>
    /// <param name="title">The error message.</param>
    public static HttpResult Failure(string title, IDictionary<string, IEnumerable<string>> detail, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        if (((int)statusCode) < 400 || ((int)statusCode) > 599)
            throw new ArgumentException("The status code must be a client or server error status code.");

        var error = new ErrorDto(title, detail);
        var result = new HttpResult(title, error, statusCode);
        return result;
    }
}
