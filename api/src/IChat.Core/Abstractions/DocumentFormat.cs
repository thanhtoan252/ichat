namespace IChat.Core.Abstractions;

public sealed record DocumentFormat
{
    public required string ContentType { get; init; }

public required IReadOnlyList<string> Extensions { get; init; }

public IReadOnlyList<byte[]> MagicBytes { get; init; } = [];

public bool RejectOnMagicMismatch { get; init; }
}
