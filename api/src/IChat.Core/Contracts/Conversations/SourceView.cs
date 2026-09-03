namespace IChat.Core.Contracts.Conversations;

public sealed class SourceView
{
    public required int Index { get; init; }

    public required Guid ChunkId { get; init; }

    public required Guid DocumentId { get; init; }

    public required string DocumentTitle { get; init; }

    public string? HeadingPath { get; init; }

    public required string Snippet { get; init; }

    public required double Score { get; init; }
}
