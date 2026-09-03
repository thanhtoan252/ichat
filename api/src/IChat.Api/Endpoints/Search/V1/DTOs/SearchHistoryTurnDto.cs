namespace IChat.Api.Endpoints.Search.V1.DTOs;

public sealed class SearchHistoryTurnDto
{
    public required string Role { get; init; }

    public required string Content { get; init; }
}
