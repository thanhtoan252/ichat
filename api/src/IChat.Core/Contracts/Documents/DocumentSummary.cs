namespace IChat.Core.Contracts.Documents;

public sealed class DocumentSummary
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required long SizeInBytes { get; init; }

    public required string Status { get; init; }

    public string? ErrorMessage { get; init; }

    public required int ChunkCount { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? IndexedAt { get; init; }
}
