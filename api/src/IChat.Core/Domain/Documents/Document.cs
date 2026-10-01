namespace IChat.Core.Domain.Documents;

public sealed class Document
{
    private readonly List<DocumentChunk> _chunks = [];

    private Document()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeInBytes { get; private set; }

    public string StoragePath { get; private set; } = string.Empty;

    public DocumentStatus Status { get; private set; }

    public string? ErrorMessage { get; private set; }

    public int ChunkCount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? IndexedAt { get; private set; }

    public IReadOnlyList<DocumentChunk> Chunks => _chunks;

    public static Document Create(string title, string fileName, string contentType, long sizeInBytes, string storagePath, DateTimeOffset createdAt)
    {
        return new Document
        {
            Id = Guid.CreateVersion7(),
            Title = title,
            FileName = fileName,
            ContentType = contentType,
            SizeInBytes = sizeInBytes,
            StoragePath = storagePath,
            Status = DocumentStatus.Pending,
            ChunkCount = 0,
            CreatedAt = createdAt
        };
    }

    public void MarkProcessing()
    {
        Status = DocumentStatus.Processing;
        ErrorMessage = null;
    }

    public void MarkIndexed(int chunkCount, DateTimeOffset indexedAt)
    {
        Status = DocumentStatus.Indexed;
        ChunkCount = chunkCount;
        IndexedAt = indexedAt;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = DocumentStatus.Failed;
        ErrorMessage = errorMessage;
    }

    public void ResetForReindex()
    {
        Status = DocumentStatus.Pending;
        ChunkCount = 0;
        IndexedAt = null;
        ErrorMessage = null;
    }
}
