namespace EvilBrains.EvilCase.Host;

internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Blazor WebAssembly compiles its runtime, which is what 'wasm-unsafe-eval' allows; the app carries
    /// no inline script, so the policy carries no hash. Inline styles are unavoidable: <c>EcTable</c>
    /// passes its column template through a custom property on the style attribute. The file preview
    /// shows downloaded bytes through a <c>blob:</c> URL in an image, a media element or a frame, which
    /// is the only place <c>blob:</c> is allowed.
    /// </summary>
    private const string ContentSecurityPolicy =
        "default-src 'self'; "
            + "base-uri 'self'; "
            + "object-src 'none'; "
            + "frame-ancestors 'none'; "
            + "form-action 'self'; "
            + "img-src 'self' blob:; "
            + "media-src 'self' blob:; "
            + "frame-src blob:; "
            + "font-src 'self'; "
            + "style-src 'self' 'unsafe-inline'; "
            + "script-src 'self' 'wasm-unsafe-eval'; "
            + "connect-src 'self'";

    private static readonly Func<object, Task> WriteHeaders = static async state =>
    {
        var headers = ((HttpResponse)state).Headers;

        headers.ContentSecurityPolicy = ContentSecurityPolicy;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
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
}
