using PDS.Domain.DTOs.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PDS.Domain.Response;

public class HttpResult<T> : HttpResult, IHttpResult<T>
{
    private readonly T? _value;

    public T Value =>
       _isSuccess
           ? _value!
           : throw new InvalidOperationException(
               "Value can not be accessed when IsSuccess is false."
           );

    protected HttpResult(T value, HttpStatusCode statusCode)
        : base(statusCode)
    {
        _value = value;
    }

    protected HttpResult(string title, ErrorDto error, HttpStatusCode statusCode)
        : base(title, error, statusCode)
    {
    }

    public static HttpResult<T> Success(T value, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        if (((int)statusCode) < 200 || ((int)statusCode) > 399)
            throw new ArgumentException("The status code must be a success status code.");

        var result = new HttpResult<T>(value, statusCode);

        return result;
    }

    public static new HttpResult<T> Failure(string title, IDictionary<string, IEnumerable<string>> detail, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        if (((int)statusCode) < 400 || ((int)statusCode) > 599)
            throw new ArgumentException("The status code must be a client or server error status code.");

        var error = new ErrorDto(title, detail);
        var result = new HttpResult<T>(title, error, statusCode);
        return result;
    }
}
