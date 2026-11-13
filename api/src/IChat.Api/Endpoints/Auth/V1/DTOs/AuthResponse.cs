namespace IChat.Api.Endpoints.Auth.V1.DTOs;

public sealed class AuthResponse
{
    public required string AccessToken { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
