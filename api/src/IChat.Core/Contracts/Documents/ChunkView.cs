namespace IChat.Core.Contracts.Documents;

public sealed class ChunkView
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
