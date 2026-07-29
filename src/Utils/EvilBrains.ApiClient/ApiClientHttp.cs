using System.Text.Json;

namespace EvilBrains.ApiClient;

/// <summary>
/// Runtime helpers consumed by generated API clients.
/// </summary>
public static class ApiClientHttp
{
    private const int ErrorBodyMaxLength = 4096;

    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web);

    public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken token)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(token);
        if (body.Length > ErrorBodyMaxLength)
            body = body[..ErrorBodyMaxLength];

        throw new ApiException(response.StatusCode, body.Length == 0 ? null : body);
    }

    public static string Format<T>(T value) => value switch
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
