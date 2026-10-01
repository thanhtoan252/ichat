namespace IChat.Core.Contracts.Auth;

using IChat.Core.Domain.Identity;

public sealed class UserView
{
    public required Guid Id { get; init; }

    public required string UserName { get; init; }

    public required string DisplayName { get; init; }

    public required UserRole Role { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
