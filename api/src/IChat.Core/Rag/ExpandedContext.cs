namespace IChat.Core.Rag;

/// <summary>
/// A contiguous context block, formed by merging the retrieved chunk with its neighbours.
/// The best-scoring retrieved anchor represents citations for the merged block.
/// </summary>
public sealed class ExpandedContext
{
    /// <summary>The top-scoring retrieved anchor that represents this merged context block.</summary>
    public required Guid CitationChunkId { get; init; }

    public required Guid DocumentId { get; init; }

    public required string DocumentTitle { get; init; }

    public string? HeadingPath { get; init; }

    public required string Text { get; init; }

    public required double Score { get; init; }

    public required int StartChunkIndex { get; init; }

    public required int EndChunkIndex { get; init; }
}
