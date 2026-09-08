namespace IChat.Api.Security;

using System.Text;
using IChat.Core.Abstractions;
using IChat.Core.Contracts.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Nằm ở tầng Api cùng chỗ với <c>AddJwtBearer</c>: tham số ký và tham số kiểm tra
/// đọc từ đúng một <see cref="JwtOptions"/>, không thể lệch nhau.
/// </summary>
public sealed class JwtAccessTokenService(
    IOptions<JwtOptions> options,
    TimeProvider timeProvider) : IAccessTokenService
{
    private readonly JwtOptions _options = options.Value;

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    public AccessToken Create(UserView user)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = credentials,
            Claims = new Dictionary<string, object>
            {
                [ClaimNames.Sub] = user.Id.ToString(),
                [ClaimNames.Name] = user.UserName,
                [ClaimNames.Role] = user.Role.ToString()
            }
        };

        return new AccessToken
        {
            Value = new JsonWebTokenHandler().CreateToken(descriptor),
            ExpiresAt = expiresAt
        };
    }
}
