namespace EvilBrains.EvilCase.Host;

internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Blazor WebAssembly compiles its runtime, which is what 'wasm-unsafe-eval' allows; the app carries
    /// no inline script, so the policy carries no hash. Inline styles are unavoidable: <c>EcTable</c>
    /// passes its column template through a custom property on the style attribute. The file preview
    /// plays and shows file content by its same-origin URL and reads a PDF with a same-origin module worker;
    /// a picture the app converts itself and an attachment of a data message shown as a picture, audio or video
    /// are <c>blob:</c> URLs, which only img-src and media-src allow; a PDF attachment reaches PDF.js as bytes.
    /// </summary>
    private const string ContentSecurityPolicy =
        "default-src 'self'; "
            + "base-uri 'self'; "
            + "object-src 'none'; "
            + "frame-ancestors 'none'; "
            + "form-action 'self'; "
            + "img-src 'self' blob:; "
            + "media-src 'self' blob:; "
            + "worker-src 'self'; "
            + "font-src 'self'; "
            + "style-src 'self' 'unsafe-inline'; "
            + "script-src 'self' 'wasm-unsafe-eval'; "
            + "connect-src 'self'";

    /// <summary>
    /// Sandboxed, so a file served inline never runs script on the app's origin.
    /// </summary>
    private const string FileContentSecurityPolicy = "sandbox; frame-ancestors 'none'";

    private static readonly Func<object, Task> WriteHeaders = static async state =>
    {
        var response = (HttpResponse)state;
        var headers = response.Headers;

        headers.ContentSecurityPolicy = IsFileContent(response.HttpContext.Request.Path) ? FileContentSecurityPolicy : ContentSecurityPolicy;
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

    // Routing ignores a trailing slash, so the match does too.
    private static bool IsFileContent(PathString path)
    {
        return path.StartsWithSegments("/api/files", StringComparison.OrdinalIgnoreCase)
            && path.Value!.TrimEnd('/').EndsWith("/content", StringComparison.OrdinalIgnoreCase);
    }
}
