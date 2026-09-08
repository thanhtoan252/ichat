namespace IChat.Core.Contracts.Auth;

/// <summary>
/// RefreshToken ở đây là token THÔ, chỉ dành cho tầng API đặt vào cookie httpOnly.
/// Nó không bao giờ được serialize vào body response.
/// </summary>
public sealed class AuthResult
{
    public required string AccessToken { get; init; }

    public required DateTimeOffset AccessTokenExpiresAt { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTimeOffset RefreshTokenExpiresAt { get; init; }

    public required UserView User { get; init; }
}
