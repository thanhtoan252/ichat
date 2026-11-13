namespace IChat.Core.Contracts.Conversations;

public sealed class SendMessageRequest
{
    public required Guid ConversationId { get; init; }

    public required string Content { get; init; }

    public string? Model { get; init; }
}
