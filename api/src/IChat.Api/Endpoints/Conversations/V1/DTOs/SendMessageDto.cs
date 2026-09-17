namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

/// <summary>ConversationId comes from the route, so it is not part of the body.</summary>
public sealed class SendMessageDto
{
    // Deliberately not marked required: a missing content must fall through to ChatService's validator
    // so the error travels over the SSE channel, instead of becoming a 400 at the deserialization layer.
    public string Content { get; init; } = string.Empty;

    public string? Model { get; init; }
}
