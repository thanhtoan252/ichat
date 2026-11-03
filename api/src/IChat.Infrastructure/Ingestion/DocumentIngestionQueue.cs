namespace IChat.Infrastructure.Ingestion;

using System.Threading.Channels;
using IChat.Core.Abstractions;

public sealed class DocumentIngestionQueue : IDocumentIngestionQueue
{

private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(
        new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

public ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(documentId, cancellationToken);
    }

public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
