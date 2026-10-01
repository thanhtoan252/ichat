namespace IChat.Core.Services.Auth;

using System.Security.Cryptography;

/// <summary>
/// Generates and hashes refresh tokens. The database only keeps the hash, so this is the only place
/// that ever sees the raw token — the returned string goes straight into an httpOnly cookie and is forgotten.
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
