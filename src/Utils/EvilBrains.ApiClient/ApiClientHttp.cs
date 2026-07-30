using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace EvilBrains.ApiClient;

/// <summary>
/// Request executor consumed by generated API clients; the generator emits one call per action.
/// Query and header values are passed as name/value pairs and null values are skipped.
/// </summary>
public static class ApiClientHttp
{
    private const int ErrorBodyMaxLength = 4096;

    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web);

    public static string Route<T>(T value) => Uri.EscapeDataString(Format(value));

    public static async Task<TResult> SendAsync<TResult>(
        HttpClient client,
        HttpMethod method,
        string url,
        CancellationToken token,
        object? body = null,
        (string Name, object? Value)[]? query = null,
        (string Name, object? Value)[]? headers = null)
    {
        using var response = await SendCoreAsync(client, method, url, body, query, headers, token);

        return await response.Content.ReadFromJsonAsync<TResult>(JsonOptions, token) ?? throw new ApiException(response.StatusCode, responseBody: null);
    }

    public static async Task<TResult?> SendNullableAsync<TResult>(
        HttpClient client,
        HttpMethod method,
        string url,
        CancellationToken token,
        object? body = null,
        (string Name, object? Value)[]? query = null,
        (string Name, object? Value)[]? headers = null)
    {
        using var response = await SendCoreAsync(client, method, url, body, query, headers, token);

        return await response.Content.ReadFromJsonAsync<TResult>(JsonOptions, token);
    }

    public static async Task SendAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        CancellationToken token,
        object? body = null,
        (string Name, object? Value)[]? query = null,
        (string Name, object? Value)[]? headers = null)
    {
        using var response = await SendCoreAsync(client, method, url, body, query, headers, token);
    }

    private static async Task<HttpResponseMessage> SendCoreAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object? body,
        (string Name, object? Value)[]? query,
        (string Name, object? Value)[]? headers,
        CancellationToken token)
    {
        using var request = new HttpRequestMessage(method, BuildUrl(url, query));
        AddHeaders(request, headers);
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);

        var response = await client.SendAsync(request, token);
        try
        {
            await EnsureSuccessAsync(response, token);
        }
        catch
        {
            response.Dispose();

            throw;
        }

        return response;
    }

    private static string BuildUrl(string url, (string Name, object? Value)[]? query)
    {
        if (query is null)
            return url;

        var builder = new StringBuilder(url);
        var separator = '?';

        foreach (var (name, value) in query)
        {
            if (value is null)
                continue;

            builder.Append(separator).Append(name).Append('=').Append(Uri.EscapeDataString(Format(value)));
            separator = '&';
        }

        return builder.ToString();
    }

    private static void AddHeaders(HttpRequestMessage request, (string Name, object? Value)[]? headers)
    {
        if (headers is null)
            return;

        foreach (var (name, value) in headers)
        {
            if (value is not null)
                request.Headers.TryAddWithoutValidation(name, Format(value));
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken token)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(token);
        if (body.Length > ErrorBodyMaxLength)
            body = body[..ErrorBodyMaxLength];

        throw new ApiException(response.StatusCode, body.Length == 0 ? null : body);
    }

    private static string Format<T>(T value) => value switch
    {
        null => "",
        string text => text,
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        DateOnly dateOnly => dateOnly.ToString("O", CultureInfo.InvariantCulture),
        TimeOnly timeOnly => timeOnly.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(format: null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };
}
