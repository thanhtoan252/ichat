namespace IChat.Api.Endpoints.Conversations.V1.Mappings;

using IChat.Api.Endpoints.Conversations.V1.DTOs;
using IChat.Core.Contracts.Conversations;

public static class ConversationMapper
{
    public static CreateConversationRequest ToServiceRequest(this CreateConversationDto dto) =>
        new()
        {
            Title = dto.Title,
            UserId = dto.UserId
        };

    public static SendMessageRequest ToServiceRequest(this SendMessageDto dto, Guid conversationId) =>
        new()
        {
            ConversationId = conversationId,
            Content = dto.Content,
            Model = dto.Model
        };

    public static ConversationResponse ToResponse(this ConversationView view) =>
        new()
        {
            Id = view.Id,
            Title = view.Title,
            UserId = view.UserId,
            CreatedAt = view.CreatedAt,
            UpdatedAt = view.UpdatedAt
        };

    public static MessageResponse ToResponse(this MessageView view) =>
        new()
        {
            Id = view.Id,
            Role = view.Role,
            Content = view.Content,
            RewrittenQuery = view.RewrittenQuery,
            Provider = view.Provider,
            Model = view.Model,
            InputTokens = view.InputTokens,
            OutputTokens = view.OutputTokens,
            LatencyMs = view.LatencyMs,
            RetrievalMs = view.RetrievalMs,
            CreatedAt = view.CreatedAt,
            Citations = view.Citations.Select(ToResponse).ToArray()
        };

    private static CitationResponse ToResponse(this CitationView view) =>
        new()
        {
            ChunkId = view.ChunkId,
            MarkerIndex = view.MarkerIndex,
            Score = view.Score,
            DocumentId = view.DocumentId,
            DocumentTitle = view.DocumentTitle,
            HeadingPath = view.HeadingPath
        };
}
