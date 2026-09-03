namespace IChat.Core.Domain.Conversations;

public sealed class Message
{
    private readonly List<MessageCitation> _citations = [];

    private Message()
    {
    }

    public Guid Id { get; private set; }

    public Guid ConversationId { get; private set; }

    public Conversation? Conversation { get; private set; }

    public MessageRole Role { get; private set; }

    public string Content { get; private set; } = string.Empty;

    /// <summary>Câu hỏi sau khi viết lại. Cột được nhìn nhiều nhất khi debug retrieval.</summary>
    public string? RewrittenQuery { get; private set; }

    public string? Provider { get; private set; }

    public string? Model { get; private set; }

    public int? InputTokens { get; private set; }

    public int? OutputTokens { get; private set; }

    public int? LatencyMs { get; private set; }

    public int? RetrievalMs { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<MessageCitation> Citations => _citations;

    public static Message CreateUser(Guid conversationId, string content, string? rewrittenQuery, int? retrievalMs, DateTimeOffset createdAt)
    {
        return new Message
        {
            Id = Guid.CreateVersion7(),
            ConversationId = conversationId,
            Role = MessageRole.User,
            Content = content,
            RewrittenQuery = rewrittenQuery,
            RetrievalMs = retrievalMs,
            CreatedAt = createdAt
        };
    }

    public static Message CreateAssistant(
        Guid conversationId,
        string content,
        string? provider,
        string? model,
        int? inputTokens,
        int? outputTokens,
        int? latencyMs,
        int? retrievalMs,
        DateTimeOffset createdAt)
    {
        return new Message
        {
            Id = Guid.CreateVersion7(),
            ConversationId = conversationId,
            Role = MessageRole.Assistant,
            Content = content,
            Provider = provider,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            LatencyMs = latencyMs,
            RetrievalMs = retrievalMs,
            CreatedAt = createdAt
        };
    }

    public void SetRetrievalMs(int retrievalMs)
    {
        RetrievalMs = retrievalMs;
    }

    public void AddCitation(MessageCitation citation)
    {
        _citations.Add(citation);
    }
}
