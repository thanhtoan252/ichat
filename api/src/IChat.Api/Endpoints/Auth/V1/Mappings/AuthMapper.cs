namespace IChat.Api.Endpoints.Auth.V1.Mappings;

using IChat.Api.Endpoints.Auth.V1.DTOs;
using IChat.Core.Contracts.Auth;

public static class AuthMapper
{
    public static LoginRequest ToServiceRequest(this LoginDto dto) =>
        new()
        {
            UserName = dto.UserName,
            Password = dto.Password
        };

    public static UserResponse ToResponse(this UserView view) =>
        new()
        {
            Id = view.Id,
            UserName = view.UserName,
            DisplayName = view.DisplayName,
            Role = view.Role.ToString(),
            IsActive = view.IsActive,
            CreatedAt = view.CreatedAt
        };

    public static AuthResponse ToResponse(this AuthResult result) =>
        new()
        {
            AccessToken = result.AccessToken,
            ExpiresAt = result.AccessTokenExpiresAt,
            User = result.User.ToResponse()
        };
}
