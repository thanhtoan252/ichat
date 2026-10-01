namespace IChat.Core.Domain.Conversations;

using IChat.Core.Domain.Documents;

public sealed class MessageCitation
{
    private MessageCitation()
    {
    }

    public Guid MessageId { get; private set; }

    public Message? Message { get; private set; }

    public Guid ChunkId { get; private set; }

    public DocumentChunk? Chunk { get; private set; }

    /// <summary>The [n] that actually appears in the answer, after verification.</summary>
    public int MarkerIndex { get; private set; }

    public double Score { get; private set; }

    public static MessageCitation Create(Guid messageId, Guid chunkId, int markerIndex, double score)
    {
        return new MessageCitation
        {
            MessageId = messageId,
            ChunkId = chunkId,
            MarkerIndex = markerIndex,
            Score = score
        };
    }
}
