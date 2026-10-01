namespace IChat.Core.Abstractions;

using IChat.Core.Common;
using IChat.Core.Contracts.Auth;

public interface IAuthService
{
    Task<Result<AuthResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<Result<AuthResult>> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task<Result> LogoutAsync(string? refreshToken, CancellationToken cancellationToken);

    Task<Result<UserView>> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
}
