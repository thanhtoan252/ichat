namespace IChat.Core.Rag;

/// <summary>
/// Khối context liên tục sau khi gộp chunk gốc với các chunk lân cận.
/// Citation vẫn trỏ về <see cref="AnchorChunkIds"/> — các chunk thực sự được retrieve.
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
