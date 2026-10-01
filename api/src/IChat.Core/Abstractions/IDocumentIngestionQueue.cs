namespace IChat.Core.Abstractions;

public interface IDocumentIngestionQueue
{
    ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken);

    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}
