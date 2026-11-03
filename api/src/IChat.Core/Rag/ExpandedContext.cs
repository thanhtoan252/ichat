namespace IChat.Core.Rag;

public sealed class ExpandedContext
{

public required Guid CitationChunkId { get; init; }

public required Guid DocumentId { get; init; }

public required string DocumentTitle { get; init; }

public string? HeadingPath { get; init; }

public required string Text { get; init; }

public required double Score { get; init; }

public required int StartChunkIndex { get; init; }

public required int EndChunkIndex { get; init; }
}
