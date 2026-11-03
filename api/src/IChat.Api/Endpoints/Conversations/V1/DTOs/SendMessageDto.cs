namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

public sealed class SendMessageDto
{

public string Content { get; init; } = string.Empty;

public string? Model { get; init; }
}
