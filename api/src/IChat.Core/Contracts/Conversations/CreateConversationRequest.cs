namespace IChat.Core.Contracts.Conversations;

public sealed class CreateConversationRequest
{
    public string? Title { get; init; }

    public string? UserId { get; init; }
}
