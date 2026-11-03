namespace IChat.Api.Security;

using System.Security.Claims;
using IChat.Core.Abstractions;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? Id =>
        Guid.TryParse(
            httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimNames.Sub),
            out var id)
            ? id
            : null;
}
