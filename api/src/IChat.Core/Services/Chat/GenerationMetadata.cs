namespace IChat.Core.Services.Chat;

/// <summary>Thông tin về lượt sinh nằm ngoài nội dung câu trả lời.</summary>
public sealed record GenerationMetadata
{
    public required string Provider { get; init; }

    public string? Model { get; init; }

    public required long LatencyMs { get; init; }

    public required long RetrievalMs { get; init; }

    public required bool Degraded { get; init; }
}
