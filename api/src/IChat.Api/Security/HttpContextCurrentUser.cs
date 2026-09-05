namespace IChat.Api.Security;

using System.Security.Claims;
using IChat.Core.Abstractions;

/// <summary>
/// Cầu nối duy nhất giữa access token đã xác thực và tầng service. Không có nơi nào
/// khác được phép quyết định "người dùng hiện tại là ai".
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? Id =>
        Guid.TryParse(
            httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimNames.Sub),
            out var id)
            ? id
            : null;
}
