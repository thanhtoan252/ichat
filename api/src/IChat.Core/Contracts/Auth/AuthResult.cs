namespace IChat.Core.Contracts.Auth;

public sealed class AuthResult
{
    public required string AccessToken { get; init; }

public required DateTimeOffset AccessTokenExpiresAt { get; init; }

public required string RefreshToken { get; init; }

public required DateTimeOffset RefreshTokenExpiresAt { get; init; }

public required UserView User { get; init; }
}
