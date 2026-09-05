namespace IChat.Api.Endpoints.Conversations.V1.DTOs;

public sealed class ConversationResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required Guid UserId { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
