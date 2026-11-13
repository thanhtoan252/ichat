namespace IChat.Core.Rag;

public sealed class RetrievalCandidate
{
    public required Guid ChunkId { get; init; }

public required Guid DocumentId { get; init; }

public required string Content { get; init; }

public string? HeadingPath { get; init; }

public required int ChunkIndex { get; init; }

public required double Score { get; init; }

public RetrievalSource? Source { get; init; }

public float[]? Embedding { get; init; }
}
