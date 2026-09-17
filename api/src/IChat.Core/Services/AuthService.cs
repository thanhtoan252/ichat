namespace IChat.Core.Services;

using System.Linq.Expressions;
using IChat.Core.Abstractions;
using IChat.Core.Common;
using IChat.Core.Contracts.Auth;
using IChat.Core.Domain.Identity;
using IChat.Core.Services.Auth;
using Microsoft.EntityFrameworkCore;

public sealed class AuthService(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IAccessTokenService accessTokenService,
    TimeProvider timeProvider) : IAuthService
{
    /// <summary>
    /// One single message for every reason a login can fail: telling "no such account" apart from
    /// "wrong password" hands an outsider a way to enumerate which usernames exist.
    /// </summary>
    private static readonly Error InvalidCredentials =
        Error.Unauthorized("The user name or password is incorrect.");

    private static readonly Expression<Func<User, UserView>> ToViewExpression =
        user => new UserView
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };

    private static readonly Func<User, UserView> ToView = ToViewExpression.Compile();

    public async Task<Result<AuthResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var userName = User.Normalize(request.UserName);
        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.UserName == userName, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthResult>(InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthResult>(Error.Unauthorized("This account has been disabled."));
        }

        return Result.Success(await IssueAsync(user, timeProvider.GetUtcNow(), cancellationToken));
    }

    /// <summary>
    /// Rotation: the token just used is revoked immediately and points at its replacement, so a stolen
    /// token works exactly once and the second use gives it away.
    /// </summary>
    public async Task<Result<AuthResult>> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = RefreshTokenFactory.Hash(refreshToken);
        var now = timeProvider.GetUtcNow();

        var stored = await dbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);

        if (stored is null || !stored.IsActiveAt(now) || stored.User is null || !stored.User.IsActive)
        {
            return Result.Failure<AuthResult>(Error.Unauthorized("The session is no longer valid."));
        }

        var issued = await IssueAsync(stored.User, now, cancellationToken, previous: stored);

        return Result.Success(issued);
    }

    public async Task<Result> LogoutAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            // Logging out without a cookie still counts as success: the client only cares about the end state.
            return Result.Success();
        }

        var hash = RefreshTokenFactory.Hash(refreshToken);
        var stored = await dbContext.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);

        if (stored is not null && stored.RevokedAt is null)
        {
            stored.Revoke(timeProvider.GetUtcNow());
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result<UserView>> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(ToViewExpression)
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserView>(Error.NotFound("User", userId));
        }

        return Result.Success(user);
    }

    private async Task<AuthResult> IssueAsync(
        User user,
        DateTimeOffset now,
        CancellationToken cancellationToken,
        RefreshToken? previous = null)
    {
        var view = ToView(user);
        var accessToken = accessTokenService.Create(view);

        var rawRefreshToken = RefreshTokenFactory.CreateRawToken();
        var refreshHash = RefreshTokenFactory.Hash(rawRefreshToken);
        var refreshExpiresAt = now.Add(accessTokenService.RefreshTokenLifetime);

        previous?.Revoke(now, refreshHash);
        dbContext.RefreshTokens.Add(RefreshToken.Issue(user.Id, refreshHash, now, refreshExpiresAt));

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult
        {
            AccessToken = accessToken.Value,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            User = view
        };
    }
}
