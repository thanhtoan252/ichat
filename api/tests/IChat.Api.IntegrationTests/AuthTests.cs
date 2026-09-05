namespace IChat.Api.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IChat.Core.Domain.Identity;
using FluentAssertions;
using Xunit;

[Collection(nameof(IChatApiCollection))]
public class AuthTests(IChatApiFactory factory)
{
    /// <summary>
    /// Không còn đăng ký công khai: tài khoản chỉ đến từ seeder, nên mọi test dưới đây
    /// dựng sẵn một hàng users rồi đăng nhập. Mật khẩu là cái EnsureUserAsync đặt.
    /// </summary>
    private static readonly object Credentials = new { userName = "alice", password = "password" };

    private async Task<HttpResponseMessage> SignInAsync(HttpClient client)
    {
        await factory.EnsureUserAsync(UserRole.User, "alice");

        return await client.PostAsJsonAsync("/api/v1/auth/login", Credentials);
    }

    /// <summary>
    /// Tắt cookie container: các test dưới đây gửi cookie bằng tay để kiểm chính xác
    /// token nào còn dùng được, nên client không được tự chèn thêm bản của nó.
    /// </summary>
    private HttpClient CookielessClient() =>
        factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

    private static string? RefreshCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(value => value.StartsWith("ichat_refresh_token=", StringComparison.Ordinal))
            : null;

    private static string CookieHeader(HttpResponseMessage response) =>
        RefreshCookie(response)!.Split(';')[0];

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task Login_ReturnsAnAccessTokenAndPutsTheRefreshTokenInACookieOnly()
    {
        await factory.ResetDatabaseAsync();
        var client = CookielessClient();

        var response = await SignInAsync(client);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await BodyAsync(response);
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();

        // Refresh token chỉ được sống trong cookie httpOnly, không bao giờ trong body.
        body.TryGetProperty("refreshToken", out _).Should().BeFalse();

        // Profile không đi kèm phiên: nó có một nguồn duy nhất là GET /auth/me.
        body.TryGetProperty("user", out _).Should().BeFalse();

        var cookie = RefreshCookie(response);
        cookie.Should().NotBeNull();
        cookie!.ToLowerInvariant().Should()
            .Contain("httponly", "script trên trang không được đọc refresh token")
            .And.Contain("path=/api/v1/auth", "cookie không nên đi kèm mọi request khác")
            .And.Contain("samesite=strict");
    }

    [Fact]
    public async Task SignUp_IsNotExposedAtAll()
    {
        // Tài khoản chỉ đến từ seeder. Nếu route này sống lại, ai cũng tự tạo được tài
        // khoản và cả mô hình "chỉ dùng seeded account" sụp theo.
        await factory.ResetDatabaseAsync();
        var client = CookielessClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new { userName = "sneaky", displayName = "S", password = "secret123" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsSameMessageAsUnknownUser()
    {
        await factory.ResetDatabaseAsync();
        var client = CookielessClient();

        await factory.EnsureUserAsync(UserRole.User, "alice");

        var wrongPassword = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { userName = "alice", password = "nope" });

        var unknownUser = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { userName = "nobody", password = "nope" });

        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownUser.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Hai câu trả lời phải không phân biệt được, nếu không thì đây là công cụ dò tài khoản.
        (await BodyAsync(wrongPassword)).GetProperty("detail").GetString()
            .Should().Be((await BodyAsync(unknownUser)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Me_WithTheLoginToken_ReturnsTheProfileThatLoginNoLongerCarries()
    {
        await factory.ResetDatabaseAsync();
        var client = CookielessClient();

        var accessToken = (await BodyAsync(await SignInAsync(client))).GetProperty("accessToken").GetString();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await BodyAsync(response);
        body.GetProperty("userName").GetString().Should().Be("alice");
        body.GetProperty("role").GetString().Should().Be("User");
    }

    /// <summary>
    /// Ghim tên claim, không phải chi tiết cài đặt: đổi ngược về URI dài của ClaimTypes là
    /// mọi request phình thêm ~160 byte, còn ICurrentUser tìm "sub" sẽ không thấy gì và
    /// im lặng coi mọi người là ẩn danh.
    /// </summary>
    [Fact]
    public async Task TheAccessToken_UsesShortJwtClaimNames()
    {
        await factory.ResetDatabaseAsync();
        var client = CookielessClient();

        var accessToken = (await BodyAsync(await SignInAsync(client))).GetProperty("accessToken").GetString();

        var payload = accessToken!.Split('.')[1].Replace('-', '+').Replace('_', '/');
        var claims = JsonDocument
            .Parse(Convert.FromBase64String(payload.PadRight((payload.Length + 3) / 4 * 4, '=')))
            .RootElement;

        claims.GetProperty("sub").GetString().Should().NotBeNullOrWhiteSpace();
        claims.GetProperty("name").GetString().Should().Be("alice");
        claims.GetProperty("role").GetString().Should().Be("User");

        claims.EnumerateObject().Should().NotContain(
            claim => claim.Name.StartsWith("http", StringComparison.Ordinal),
            "claim URI dài đi kèm mọi request");
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        await factory.ResetDatabaseAsync();
        var client = CookielessClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_RotatesTheToken_AndTheOldOneStopsWorking()
    {
        await factory.ResetDatabaseAsync();
        var client = CookielessClient();

        var signedIn = await SignInAsync(client);
        var firstCookie = CookieHeader(signedIn);

        var refreshed = await SendWithCookieAsync(client, firstCookie);
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondCookie = CookieHeader(refreshed);
        secondCookie.Should().NotBe(firstCookie);

        // Token cũ đã bị thu hồi ngay khi đổi: một token bị đánh cắp chỉ dùng được một lần.
        (await SendWithCookieAsync(client, firstCookie)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        (await SendWithCookieAsync(client, secondCookie)).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        await factory.ResetDatabaseAsync();
        var client = CookielessClient();

        var signedIn = await SignInAsync(client);
        var cookie = CookieHeader(signedIn);
        var accessToken = (await BodyAsync(signedIn)).GetProperty("accessToken").GetString();

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logout.Headers.Add("Cookie", cookie);
        logout.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        (await client.SendAsync(logout)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await SendWithCookieAsync(client, cookie)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        await factory.ResetDatabaseAsync();
        var client = CookielessClient();

        var response = await client.PostAsync("/api/v1/auth/refresh", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static Task<HttpResponseMessage> SendWithCookieAsync(HttpClient client, string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add("Cookie", cookie);

        return client.SendAsync(request);
    }
}
