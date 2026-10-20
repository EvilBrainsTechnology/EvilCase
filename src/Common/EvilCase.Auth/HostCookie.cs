using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// Reads a <c>__Host-</c> cookie by its exact name. <c>Request.Cookies</c> ignores case, so a sibling subdomain
/// could plant <c>__HOST-…</c>, which a browser does not hold to the prefix, and have it read as the real one.
/// </summary>
public static class HostCookie
{
    public static string? Read(HttpRequest request, string name)
    {
        if (!CookieHeaderValue.TryParseList(request.Headers.Cookie, out var cookies))
            return null;

        // A host-only cookie exists once per name; a second one was planted, and neither is trusted.
        var matches = cookies
            .Where(cookie => cookie.Name.Equals(name, StringComparison.Ordinal))
            .Take(2)
            .ToList();

        return matches is [{ Value.Length: > 0 } only] ? only.Value.ToString() : null;
    }
}
