namespace IChat.Core.Contracts.Conversations;

public sealed class MessageView
{
    public required Guid Id { get; init; }

    public required string Role { get; init; }

    public required string Content { get; init; }

    public string? RewrittenQuery { get; init; }

    public string? Provider { get; init; }

    public string? Model { get; init; }

    public int? InputTokens { get; init; }

    public int? OutputTokens { get; init; }

    public int? LatencyMs { get; init; }

    public int? RetrievalMs { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required IReadOnlyList<CitationView> Citations { get; init; }
}
