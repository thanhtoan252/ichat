namespace IChat.Api.Endpoints.Documents.V1.DTOs;

/// <summary>
/// Công cụ debug ingestion: headingPath và embeddedText là thứ cần nhìn đầu tiên
/// khi nghi chunker hỏng. Không bao giờ trả vector embedding.
/// </summary>
public sealed class DocumentChunkResponse
{
    public required Guid Id { get; init; }

    public required int ChunkIndex { get; init; }

    public required string Content { get; init; }

    public string? HeadingPath { get; init; }

    public required string EmbeddedText { get; init; }

    public required int TokenCount { get; init; }

    public required string EmbeddingModel { get; init; }

    public required int EmbeddingDimensions { get; init; }

    public required string Metadata { get; init; }
}
