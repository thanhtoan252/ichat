namespace IChat.Core.Abstractions;

public sealed class TextChunk
{
    public required int Index { get; init; }

    public required string Content { get; init; }

    public string? HeadingPath { get; init; }

    public required string EmbeddedText { get; init; }

    public required int TokenCount { get; init; }

    public required IReadOnlyDictionary<string, object?> Metadata { get; init; }
}
