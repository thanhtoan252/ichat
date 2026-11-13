namespace IChat.Core.Services.Auth;

using System.Security.Cryptography;

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
