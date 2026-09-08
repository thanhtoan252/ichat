namespace IChat.Api.Endpoints.Documents.V1.DTOs;

/// <summary>Không lộ StoragePath: đường dẫn nội bộ không phải chuyện của client.</summary>
public sealed class DocumentResponse
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
