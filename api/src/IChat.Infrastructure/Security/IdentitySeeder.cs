namespace IChat.Infrastructure.Security;

using IChat.Core.Abstractions;
using IChat.Core.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class IdentitySeeder(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<IdentitySeeder> logger) : BackgroundService
{
    private sealed record DefaultAccount(string UserName, string Password, string DisplayName, UserRole Role);

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

            logger.LogError(exception, "Could not seed the default accounts.");
        }
    }
}
