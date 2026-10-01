namespace IChat.Api.Endpoints.Documents.V1.DTOs;

/// <summary>
/// An ingestion debugging tool: headingPath and embeddedText are the first things to look at
/// when the chunker is suspect. It never returns the embedding vector.
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
