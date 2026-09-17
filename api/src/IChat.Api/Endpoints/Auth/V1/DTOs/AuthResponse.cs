namespace IChat.Api.Endpoints.Auth.V1.DTOs;

/// <summary>
/// Carries only the secret half of a session. Two things are DELIBERATELY absent:
/// the refresh token — it travels in an httpOnly cookie, so a script on the page cannot
/// read it even through XSS; and the user profile — read from <c>GET /api/v1/auth/me</c>,
/// where the server derives the identity from the <c>sub</c> claim so the client never
/// has to decode the token itself.
/// </summary>
public sealed class AuthResponse
{
    public required string AccessToken { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
