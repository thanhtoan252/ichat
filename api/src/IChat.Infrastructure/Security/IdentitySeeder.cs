namespace IChat.Infrastructure.Security;

using IChat.Core.Abstractions;
using IChat.Core.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Creates the default accounts if the database does not have them yet.
/// </summary>
public sealed class IdentitySeeder(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<IdentitySeeder> logger) : BackgroundService
{
    private sealed record DefaultAccount(string UserName, string Password, string DisplayName, UserRole Role);

    // A showcase project: two default accounts are hard-coded so a fresh clone runs straight away.
    // A real system would load them from a secret store and force a password change on first login.
    private static readonly DefaultAccount[] DefaultAccounts =
    [
        new("admin", "admin", "Administrator", UserRole.Admin),
        new("user", "user", "Demo User", UserRole.User)
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            // Each account is considered on its own rather than asking "is the users table empty": a database
            // that already has an admin still picks up a demo account newly added here.
            var existing = await dbContext.Users
                .Select(user => user.UserName)
                .ToListAsync(stoppingToken);

            var created = new List<string>();

            foreach (var account in DefaultAccounts.Where(item => !existing.Contains(item.UserName)))
            {
                dbContext.Users.Add(User.Create(
                    account.UserName,
                    account.DisplayName,
                    passwordHasher.Hash(account.Password),
                    account.Role,
                    timeProvider.GetUtcNow()));

                created.Add(account.UserName);
            }

            if (created.Count == 0)
            {
                return;
            }

            await dbContext.SaveChangesAsync(stoppingToken);

            logger.LogWarning(
                "Seeded the default accounts {UserNames} with well-known showcase passwords. Change them before exposing this instance.",
                string.Join(", ", created));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A database that is not ready yet must not bring the host down: the health check reports that.
            logger.LogError(exception, "Could not seed the default accounts.");
        }
    }
}
