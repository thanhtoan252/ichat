namespace IChat.Core.Services.Chat;

/// <summary>A finished answer, enough to persist it and to build the "done" payload.</summary>
public sealed record GeneratedAnswer
{
    public required string Text { get; init; }

    public required bool Interrupted { get; init; }

    public int? InputTokens { get; init; }

    public int? OutputTokens { get; init; }
}
