namespace IChat.Api.Security;

/// <summary>
/// Short claim names, per the JWT standard. The token used to carry the long <c>ClaimTypes</c> URIs
/// (~160 bytes for those three names alone) on EVERY request. They live in one place because the
/// signing side (<see cref="JwtAccessTokenService"/>), the validating side (AddJwtBearer) and the
/// reading side (<see cref="HttpContextCurrentUser"/>) drifting by one character means a silent 401/403.
/// </summary>
public static class ClaimNames
{
    public const string Sub = "sub";

    public const string Name = "name";

    public const string Role = "role";
}
