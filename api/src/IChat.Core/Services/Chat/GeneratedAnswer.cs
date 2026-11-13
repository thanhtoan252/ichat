namespace IChat.Core.Services.Chat;

public sealed record GeneratedAnswer
{
    public required string Text { get; init; }

    public required bool Interrupted { get; init; }

    public int? InputTokens { get; init; }

    public int? OutputTokens { get; init; }
}
