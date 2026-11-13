namespace IChat.Core.Abstractions;

using IChat.Core.Contracts.Auth;

public interface IAccessTokenService
{
    AccessToken Create(UserView user);

    TimeSpan RefreshTokenLifetime { get; }
}
