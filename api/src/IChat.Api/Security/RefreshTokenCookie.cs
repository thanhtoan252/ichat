namespace IChat.Api.Security;

/// <summary>
/// Refresh token sống trong cookie httpOnly chứ không trong localStorage: script trên
/// trang không đọc được nó, nên một lỗ hổng XSS không đổi được thành phiên đăng nhập
/// vĩnh viễn. Path bó hẹp vào /api/v1/auth để cookie không đi kèm mọi request khác.
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
            // UI và API dùng chung origin (dev proxy, nginx ở production) nên Strict không
            // cản trở gì; Secure bám theo scheme thật để http://localhost vẫn chạy được.
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = Path,
            Expires = expiresAt
        };
}
