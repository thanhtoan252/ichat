namespace IChat.Core.Services.Chat;

using Microsoft.Extensions.AI;

/// <summary>The result of a chat turn's first half: the history, and the question used to search.</summary>
public sealed record ChatTurnQuery
{
    public required IReadOnlyList<ChatMessage> History { get; init; }

    public required string RewrittenQuery { get; init; }
}
