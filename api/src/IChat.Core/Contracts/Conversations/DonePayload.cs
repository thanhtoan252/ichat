namespace IChat.Core.Contracts.Conversations;

/// <summary>Chỉ chứa nguồn mà câu trả lời THỰC SỰ trích dẫn, đã qua kiểm chứng marker.</summary>
public sealed class DonePayload
{
    public required Guid MessageId { get; init; }

    public required IReadOnlyList<CitationPayload> Citations { get; init; }

    public string? Provider { get; init; }

    public string? Model { get; init; }

    public int? InputTokens { get; init; }

    public int? OutputTokens { get; init; }

    public required long LatencyMs { get; init; }

    public required long RetrievalMs { get; init; }

    public required bool Degraded { get; init; }

    public required bool Interrupted { get; init; }
}
