namespace IChat.Core.Rag;

/// <summary>One source, numbered [n], that really is part of the context sent to the model.</summary>
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
