namespace IChat.Core.Abstractions;

using IChat.Core.Contracts.Auth;

public interface IAccessTokenService
{
    AccessToken Create(UserView user);

    /// <summary>Refresh token lifetime, decided by the same place that configures JWT.</summary>
    TimeSpan RefreshTokenLifetime { get; }
}
