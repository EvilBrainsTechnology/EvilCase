namespace EvilBrains.EvilCase.Host;

/// <summary>
/// Baseline security headers on every response. Nothing in the app renders raw HTML today; the policy is
/// what keeps a future sink from mattering.
/// </summary>
internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Blazor WebAssembly compiles its runtime, which is what 'wasm-unsafe-eval' allows; the hash covers
    /// the inline theme script of index.html and <c>SecurityHeadersTests</c> recomputes it from the served
    /// file. Inline styles are unavoidable: TabBlazor and Popper position elements through the style
    /// attribute. Images allow data: URIs because the vendored Tabler CSS embeds its icons as such.
    /// </summary>
    private const string ContentSecurityPolicy =
        "default-src 'self'; "
            + "base-uri 'self'; "
            + "object-src 'none'; "
            + "frame-ancestors 'none'; "
            + "form-action 'self'; "
            + "img-src 'self' data:; "
            + "font-src 'self'; "
            + "style-src 'self' 'unsafe-inline'; "
            + "script-src 'self' 'wasm-unsafe-eval' 'sha256-Twd7JFh40ZBLj45GN0frQiMZ6sELOfQJW1roNApIVxk='; "
            + "connect-src 'self'";

    private static readonly Func<object, Task> WriteHeaders = static state =>
    {
        var headers = ((HttpResponse)state).Headers;

        headers.ContentSecurityPolicy = ContentSecurityPolicy;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        return Task.CompletedTask;
    };

    public Task Invoke(HttpContext context)
    {
        // Written when the response starts rather than here: the exception handler clears the response
        // before it writes the problem details, which would drop headers set on the way in.
        context.Response.OnStarting(WriteHeaders, context.Response);

        return next(context);
    }
}
