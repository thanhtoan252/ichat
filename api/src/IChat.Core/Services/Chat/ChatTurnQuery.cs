namespace IChat.Core.Services.Chat;

using Microsoft.Extensions.AI;

public sealed record ChatTurnQuery
{
    public required IReadOnlyList<ChatMessage> History { get; init; }

    public required string RewrittenQuery { get; init; }
}
