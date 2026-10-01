namespace IChat.Core.Abstractions;

/// <summary>
/// Everything known about a format lives next to that format's parser, so adding a new one means
/// touching a single place: write the parser and declare its Format.
/// </summary>
public sealed record DocumentFormat
{
    public required string ContentType { get; init; }

    public required IReadOnlyList<string> Extensions { get; init; }

    public IReadOnlyList<byte[]> MagicBytes { get; init; } = [];

    /// <summary>Extension matches but the magic bytes do not => reject, do not guess (the fake .docx case).</summary>
    public bool RejectOnMagicMismatch { get; init; }
}
