namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

/// <summary>
/// No UserId: the owner comes from the access token, never from the body — otherwise anyone
/// could create a conversation in someone else's name.
/// </summary>
public sealed class CreateConversationDto
{
    public string? Title { get; init; }
}
