namespace IChat.Core.Contracts.Auth;

/// <summary>
/// RefreshToken here is the RAW token, meant only for the API layer to put into an httpOnly cookie.
/// It is never serialized into a response body.
/// </summary>
public sealed class AuthResult
{
    public required string AccessToken { get; init; }

    public required DateTimeOffset AccessTokenExpiresAt { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTimeOffset RefreshTokenExpiresAt { get; init; }

    public required UserView User { get; init; }
}
