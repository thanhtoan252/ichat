namespace IChat.Api.Endpoints.Auth.V1.DTOs;

/// <summary>
/// Chỉ có phần bí mật của phiên. Hai thứ CỐ TÌNH vắng mặt:
/// refresh token — chỉ đi trong cookie httpOnly nên script trên trang không đọc được
/// kể cả khi có XSS; và profile người dùng — đọc qua <c>GET /api/v1/auth/me</c>, nơi
/// server tự suy ra danh tính từ claim <c>sub</c> nên client không phải tự giải mã token.
/// </summary>
public sealed class AuthResponse
{
    public required string AccessToken { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
