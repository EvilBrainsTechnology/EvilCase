namespace EvilBrains.EvilCase.Host;

internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Blazor WebAssembly compiles its runtime, which is what 'wasm-unsafe-eval' allows; the app carries
    /// no inline script, so the policy carries no hash. Inline styles are unavoidable: <c>EcTable</c>
    /// passes its column template through a custom property on the style attribute. The file preview
    /// frames, plays and shows file content by its same-origin URL; only a picture the app converts
    /// itself is a <c>blob:</c> URL, the only place <c>blob:</c> is allowed.
    /// </summary>
    private const string ContentSecurityPolicy =
        "default-src 'self'; "
            + "base-uri 'self'; "
            + "object-src 'none'; "
            + "frame-ancestors 'none'; "
            + "form-action 'self'; "
            + "img-src 'self' blob:; "
            + "media-src 'self'; "
            + "frame-src 'self'; "
            + "font-src 'self'; "
            + "style-src 'self' 'unsafe-inline'; "
            + "script-src 'self' 'wasm-unsafe-eval'; "
            + "connect-src 'self'";

    /// <summary>
    /// The file content is the one response the app frames itself, for the PDF preview.
    /// </summary>
    private static readonly string FileContentSecurityPolicy = ContentSecurityPolicy.Replace("frame-ancestors 'none'", "frame-ancestors 'self'", StringComparison.Ordinal);

    private static readonly Func<object, Task> WriteHeaders = static async state =>
    {
        var response = (HttpResponse)state;
        var headers = response.Headers;
        var frameable = IsFileContent(response.HttpContext.Request.Path);

        headers.ContentSecurityPolicy = frameable ? FileContentSecurityPolicy : ContentSecurityPolicy;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = frameable ? "SAMEORIGIN" : "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    };

    public async Task Invoke(HttpContext context)
    {
        // Written when the response starts rather than here: the exception handler clears the response
        // before it writes the problem details, which would drop headers set on the way in.
        context.Response.OnStarting(WriteHeaders, context.Response);

        await next(context);
    }

    private static bool IsFileContent(PathString path)
    {
        return path.StartsWithSegments("/api/files", StringComparison.OrdinalIgnoreCase) && path.Value!.EndsWith("/content", StringComparison.OrdinalIgnoreCase);
    }
}
