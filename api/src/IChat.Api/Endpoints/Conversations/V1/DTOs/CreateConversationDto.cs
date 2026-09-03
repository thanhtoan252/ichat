namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

public sealed class CreateConversationDto
{
    public string? Title { get; init; }

    public string? UserId { get; init; }
}
