namespace IChat.Core.Rag;

/// <summary>
/// A contiguous context block, formed by merging the retrieved chunk with its neighbours.
/// Citations still point back to <see cref="AnchorChunkIds"/> — the chunks that were actually retrieved.
/// </summary>
public sealed class ExpandedContext
{
    public required IReadOnlyList<Guid> AnchorChunkIds { get; init; }

    public required Guid DocumentId { get; init; }

    public required string DocumentTitle { get; init; }

    public string? HeadingPath { get; init; }

    public required string Text { get; init; }

    public required double Score { get; init; }

    public required int StartChunkIndex { get; init; }

    public required int EndChunkIndex { get; init; }
}
