namespace IChat.Core.Abstractions;

using IChat.Core.Contracts.Auth;

public interface IAccessTokenService
{
    AccessToken Create(UserView user);

    /// <summary>Hạn sống của refresh token, do cùng một nơi cấu hình JWT quyết định.</summary>
    TimeSpan RefreshTokenLifetime { get; }
}
