namespace IChat.Api.Security;

using System.Security.Claims;
using IChat.Core.Abstractions;

/// <summary>
/// The only bridge between the validated access token and the service layer. Nowhere else is
/// allowed to decide who the current user is.
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
