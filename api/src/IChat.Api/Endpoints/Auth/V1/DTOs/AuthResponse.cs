namespace IChat.Api.Endpoints.Auth.V1.DTOs;

/// <summary>
/// Refresh token CỐ TÌNH không có ở đây: nó chỉ đi trong cookie httpOnly, nên script
/// trên trang không đọc được kể cả khi có lỗ hổng XSS.
/// </summary>
public sealed class AuthResponse
{
    public required string AccessToken { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required UserResponse User { get; init; }
}
