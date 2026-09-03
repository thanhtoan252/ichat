namespace IChat.Core.Contracts.Conversations;

public sealed class ConversationView
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public string? UserId { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
