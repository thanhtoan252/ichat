namespace IChat.Core.Services.Chat;

/// <summary>Câu trả lời đã sinh xong, đủ để lưu và để dựng payload "done".</summary>
public sealed record GeneratedAnswer
{
    public required string Text { get; init; }

    public required bool Interrupted { get; init; }

    public int? InputTokens { get; init; }

    public int? OutputTokens { get; init; }
}
