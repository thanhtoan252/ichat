namespace IChat.Api.Endpoints.Auth.V1.DTOs;

public sealed class LoginDto
{
    public string UserName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
