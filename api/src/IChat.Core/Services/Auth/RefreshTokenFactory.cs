namespace IChat.Core.Services.Auth;

using System.Security.Cryptography;

/// <summary>
/// Sinh và băm refresh token. Database chỉ giữ bản băm, nên đây là nơi duy nhất
/// biết token thô — chuỗi trả về đi thẳng vào cookie httpOnly rồi bị quên.
/// </summary>
public static class RefreshTokenFactory
{
    private const int TokenSizeInBytes = 32;

    public static string CreateRawToken() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenSizeInBytes));

    public static string Hash(string rawToken) =>
        Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
