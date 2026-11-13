namespace IChat.Api.Security;

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

Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = Path,
            Expires = expiresAt
        };
}
