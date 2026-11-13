namespace IChat.Core.Contracts.Conversations;

public sealed class CitationView
{
    public required Guid ChunkId { get; init; }

    public required int MarkerIndex { get; init; }

    public required double Score { get; init; }

    public required Guid DocumentId { get; init; }

    public required string DocumentTitle { get; init; }

    public string? HeadingPath { get; init; }
}
