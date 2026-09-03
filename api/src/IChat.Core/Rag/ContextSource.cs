namespace IChat.Core.Rag;

/// <summary>Một nguồn đã được đánh số [n] và thực sự nằm trong context gửi cho model.</summary>
public sealed class ContextSource
{
    public required int Index { get; init; }

    public required IReadOnlyList<Guid> AnchorChunkIds { get; init; }

    public required Guid DocumentId { get; init; }

    public required string DocumentTitle { get; init; }

    public string? HeadingPath { get; init; }

    public required string Text { get; init; }

    public required double Score { get; init; }
}
