// Standard exception constructors are omitted on purpose: the exception is thrown only by generated
// clients and a response status code is always available.
#pragma warning disable RCS1194

using System.Net;

namespace EvilBrains.ApiClient;

public sealed class ApiException(HttpStatusCode statusCode, string? responseBody, string? message = null)
    : Exception(message ?? string.Create(CultureInfo.InvariantCulture, $"API request failed with status code {(int)statusCode} ({statusCode})."))
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public string? ResponseBody { get; } = responseBody;
}
