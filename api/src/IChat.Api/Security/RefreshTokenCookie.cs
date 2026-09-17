namespace IChat.Api.Security;

/// <summary>
/// The refresh token lives in an httpOnly cookie rather than in localStorage: a script on the page
/// cannot read it, so an XSS hole cannot be turned into a permanent session. The path is narrowed
/// to /api/v1/auth so the cookie does not ride along with every other request.
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "ichat_refresh_token";

    private const string Path = "/api/v1/auth";

    public static void Append(HttpContext httpContext, string token, DateTimeOffset expiresAt)
    {
        httpContext.Response.Cookies.Append(Name, token, BuildOptions(httpContext, expiresAt));
    }

    public static void Delete(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(Name, BuildOptions(httpContext, expiresAt: null));
    }

    public static string? Read(HttpContext httpContext) =>
        httpContext.Request.Cookies.TryGetValue(Name, out var token) ? token : null;

    private static CookieOptions BuildOptions(HttpContext httpContext, DateTimeOffset? expiresAt) =>
        new()
        {
            HttpOnly = true,
            // The UI and the API share one origin (dev proxy, nginx in production), so Strict costs
            // nothing; Secure follows the real scheme so http://localhost still works.
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = Path,
            Expires = expiresAt
        };
}
