namespace IChat.Api.Endpoints.Search.V1.DTOs;

public sealed class SearchHitResponse
{
    public required Guid ChunkId { get; init; }

    public required Guid DocumentId { get; init; }

    public string? HeadingPath { get; init; }

    public required string Snippet { get; init; }

    public required int ChunkIndex { get; init; }

    public required double Score { get; init; }
}
