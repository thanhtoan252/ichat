namespace IChat.Api.Endpoints.Auth.V1.DTOs;

public sealed class UserResponse
{
    public required Guid Id { get; init; }

    public required string UserName { get; init; }

    public required string DisplayName { get; init; }

    public required string Role { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
